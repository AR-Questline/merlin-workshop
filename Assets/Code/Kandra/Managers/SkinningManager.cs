using System;
using Awaken.Kandra.Data;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public unsafe class SkinningManager : IMemorySnapshotProvider {
        static readonly int GlobalSkinnedVerticesBufferId = Shader.PropertyToID("_GlobalSkinnedVertices");
        static readonly int GlobalRenderersDataBufferId = Shader.PropertyToID("_GlobalRenderersData");
        static readonly int GlobalSkinningVerticesDataBufferId = Shader.PropertyToID("_GlobalSkinningVerticesData");
        static readonly int GlobalPreviousPositionsBufferId = Shader.PropertyToID("_GlobalPreviousPositions");

        static readonly int VertexOffsetId = Shader.PropertyToID("_VertexOffset");
        static readonly int SkinningVerticesDataId = Shader.PropertyToID("_SkinningVerticesData");
        static readonly int RenderersDataId = Shader.PropertyToID("_RenderersData");

        static readonly int OutputVerticesId = Shader.PropertyToID("_OutputVertices");
        static readonly int VertexCountId = Shader.PropertyToID("_VertexCount");
        static readonly int ToCopyVerticesId = Shader.PropertyToID("_ToCopyVertices");
        static readonly int PreviousVerticesId = Shader.PropertyToID("_PreviousVertices");

        readonly ComputeShader _skinningComputeShader;
        readonly int _skinningKernel;
        readonly int _copyKernel;
        readonly uint _xGroupSize;

        GraphicsBuffer _skinningVerticesDataBuffer;
        GraphicsBuffer _renderersDataBuffer;
        GraphicsBuffer _outputVerticesBuffer;
        GraphicsBuffer _previousPositions;

        UnsafeArray<RegisteredRenderer> _registeredRenderers;
        MemoryBookkeeper _memoryRegions;

        public float FillPercentage => (float) VerticesCount / _memoryRegions.Capacity;
        public GraphicsBuffer OutputVerticesBuffer => _outputVerticesBuffer;
        uint VerticesCount => _memoryRegions.LastBinStart;

        public SkinningManager(ComputeShader skinningShader) {
            var skinnedVerticesCapacity = KandraRendererManager.FinalSkinnedVerticesCapacity;
            var maxRenderers = KandraRendererManager.FinalRenderersCapacity;

            _skinningComputeShader = skinningShader;
            _skinningKernel = _skinningComputeShader.FindKernel("CSSkinning");
            _copyKernel = _skinningComputeShader.FindKernel("CSCopyPreviousPositions");
            _skinningComputeShader.GetKernelThreadGroupSizes(_skinningKernel, out _xGroupSize, out _, out _);

            _skinningVerticesDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, skinnedVerticesCapacity, sizeof(SkinningVerticesDatum));
            _renderersDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxRenderers, sizeof(RendererDatum));
            _outputVerticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, skinnedVerticesCapacity, sizeof(CompressedVertex));
            _previousPositions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, skinnedVerticesCapacity, sizeof(float3));

            _skinningComputeShader.SetInt(VertexCountId, 0);

            _registeredRenderers = new UnsafeArray<RegisteredRenderer>((uint)maxRenderers, ARAlloc.Persistent);
            _memoryRegions = new MemoryBookkeeper("Verts skinned", (uint)skinnedVerticesCapacity, maxRenderers / 3, ARAlloc.Persistent);

            //EnsureBuffers();
        }

        public void Dispose() {
            _skinningVerticesDataBuffer?.Dispose();
            _renderersDataBuffer?.Dispose();
            _outputVerticesBuffer?.Dispose();
            _previousPositions?.Dispose();

            if (_registeredRenderers.IsCreated) {
                _registeredRenderers.Dispose();
                _memoryRegions.Dispose();
            }
        }

        public bool CanRegister(in MemoryBookkeeper.MemoryRegion meshMemory, out MemoryBookkeeper.MemoryRegion memoryDestination, ref string errorMessage) {
            var success = _memoryRegions.FindFreeRegion(meshMemory.length, out memoryDestination);
            if (!success) {
                errorMessage = BrokenKandraMessage.AppendMessageInfo(errorMessage, meshMemory.length, _memoryRegions);
            }
            return success;
        }

        public void Register(uint slot, in MemoryBookkeeper.MemoryRegion rendererRegion, in MemoryBookkeeper.MemoryRegion meshMemory, uint bonesOffset) {
            _memoryRegions.TakeFreeRegion(rendererRegion);

            Asserts.AreEqual(meshMemory.length, rendererRegion.length, "Mesh memory length must match renderer region length for skinning registration.");

            _registeredRenderers[slot] = new RegisteredRenderer {
                memory = rendererRegion
            };

            var verticesStart = (int)rendererRegion.start;

            var verticesData = new UnsafeArray<SkinningVerticesDatum>(rendererRegion.length, ARAlloc.Temp, NativeArrayOptions.UninitializedMemory);

            for (var i = 0u; i < meshMemory.length; ++i) {
                verticesData[i] = new SkinningVerticesDatum {
                    vertexIndexAndRendererIndex = i | (slot << 16)
                };
            }
            _skinningVerticesDataBuffer.SetData(verticesData.AsNativeArray(), 0, verticesStart, (int)verticesData.Length);
            verticesData.Dispose();

            var rendererDataArray = new UnsafeArray<RendererDatum>(1, ARAlloc.Temp, NativeArrayOptions.UninitializedMemory);
            rendererDataArray[0] = new RendererDatum {
                meshStart = meshMemory.start,
                bonesStart = bonesOffset
            };
            _renderersDataBuffer.SetData(rendererDataArray.AsNativeArray(), 0, (int)slot, 1);
            rendererDataArray.Dispose();
        }

        public void Unregister(uint slot) {
            var renderer = _registeredRenderers[slot];
            _memoryRegions.Return(renderer.memory);
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            commandBuffer.SetComputeBufferParam(_skinningComputeShader, _skinningKernel, SkinningVerticesDataId, _skinningVerticesDataBuffer);
            commandBuffer.SetComputeBufferParam(_skinningComputeShader, _skinningKernel, RenderersDataId, _renderersDataBuffer);
            commandBuffer.SetComputeBufferParam(_skinningComputeShader, _skinningKernel, OutputVerticesId, _outputVerticesBuffer);

            commandBuffer.SetComputeBufferParam(_skinningComputeShader, _copyKernel, ToCopyVerticesId, _outputVerticesBuffer);
            commandBuffer.SetComputeBufferParam(_skinningComputeShader, _copyKernel, PreviousVerticesId, _previousPositions);

            commandBuffer.SetGlobalBuffer(GlobalSkinningVerticesDataBufferId, _skinningVerticesDataBuffer);
            commandBuffer.SetGlobalBuffer(GlobalRenderersDataBufferId, _renderersDataBuffer);
            commandBuffer.SetGlobalBuffer(GlobalSkinnedVerticesBufferId, _outputVerticesBuffer);
            commandBuffer.SetGlobalBuffer(GlobalPreviousPositionsBufferId, _previousPositions);

            var verticesCount = VerticesCount;
            var asIntVerticesCount = UnsafeUtility.As<uint, int>(ref verticesCount);
            commandBuffer.SetComputeIntParam(_skinningComputeShader, VertexCountId, asIntVerticesCount);
        }

        public void RunCopyPrevious(CommandBuffer commandBuffer) {
            RunComputeKernel(commandBuffer, _copyKernel);
        }

        public void RunSkinning(CommandBuffer commandBuffer) {
            RunComputeKernel(commandBuffer, _skinningKernel);
        }

        void RunComputeKernel(CommandBuffer commandBuffer, int kernel) {
            const int MaxDispatches = 60_000;

            var verticesCount = VerticesCount;
            if (verticesCount > 0) {
                var dispatchCount = Mathf.CeilToInt((float)verticesCount / _xGroupSize);
                var vertexOffset = 0;
                while (dispatchCount > 0) {
                    var dispatches = Mathf.Min(dispatchCount, MaxDispatches);
                    commandBuffer.SetComputeIntParam(_skinningComputeShader, VertexOffsetId, vertexOffset);
                    commandBuffer.DispatchCompute(_skinningComputeShader, kernel, dispatches, 1, 1);
                    dispatchCount -= dispatches;
                    vertexOffset += dispatches * (int)_xGroupSize;
                }
            }
        }

        public uint GetVertexStart(uint slot) {
            return _registeredRenderers[slot].memory.start;
        }

        public bool TryGetSkinnedVerticesMemory(uint slot, out MemoryBookkeeper.MemoryRegion memory) {
            if (KandraRendererManager.IsInvalidId(slot) || KandraRendererManager.IsWaitingId(slot)) {
                memory = default;
                return false;
            }
            var uSlot = KandraRendererManager.USlot(slot);
            memory = _registeredRenderers[uSlot].memory;
            return true;
        }

        public ulong GetMemoryUsageFor(uint slot) {
            if (KandraRendererManager.IsInvalidId(slot) || KandraRendererManager.IsWaitingId(slot)) {
                return 0;
            }
            var uSlot = KandraRendererManager.USlot(slot);

            var memory = _registeredRenderers[uSlot].memory;
            return memory.length * (ulong)sizeof(SkinningVerticesDatum) +
                   memory.length * (ulong)sizeof(CompressedVertex) +
                   (ulong)sizeof(RendererDatum);
        }

        struct RegisteredRenderer {
            public MemoryBookkeeper.MemoryRegion memory;
        }

        struct SkinningVerticesDatum {
            public uint vertexIndexAndRendererIndex;
        }

        struct RendererDatum {
            public uint meshStart;
            public uint bonesStart;
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 5;

            var registeredRenderers = KandraRendererManager.Instance.RegisteredRenderers;
            MemorySnapshotUtils.TakeSnapshot("SkinningVerticesDataBuffer", _skinningVerticesDataBuffer, VerticesCount, memoryBuffer.Slice(0, 1));
            MemorySnapshotUtils.TakeSnapshot("RenderersDataDataBuffer", _renderersDataBuffer, registeredRenderers, memoryBuffer.Slice(1, 1));
            MemorySnapshotUtils.TakeSnapshot("OutputVerticesBuffer", _outputVerticesBuffer, VerticesCount, memoryBuffer.Slice(2, 1));
            MemorySnapshotUtils.TakeSnapshot("PreviousPositionsBuffer", _previousPositions, VerticesCount, memoryBuffer.Slice(3, 1));
            _memoryRegions.GetMemorySnapshot(memoryBuffer.Slice(4, 1));

            var selfSize = _registeredRenderers.Length * sizeof(RegisteredRenderer);
            var usedSize = registeredRenderers * sizeof(RegisteredRenderer);

            ownPlace.Span[0] = new(nameof(SkinningManager), selfSize, usedSize, memoryBuffer[..childrenCount]);

            return childrenCount;
        }

        public readonly struct EditorAccess {
            readonly SkinningManager _manager;

            public ref readonly MemoryBookkeeper SkinVertsMemory => ref _manager._memoryRegions;

            public EditorAccess(SkinningManager manager) {
                _manager = manager;
            }

            public static EditorAccess Get() {
                return new EditorAccess(KandraRendererManager.Instance.SkinningManager);
            }
        }
    }
}