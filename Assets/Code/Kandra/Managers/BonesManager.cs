using System;
using Awaken.Kandra.Data;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public unsafe class BonesManager : IMemorySnapshotProvider {
        static readonly int BonesId = Shader.PropertyToID("_Bones");
        static readonly int SkinningBonesDataId = Shader.PropertyToID("_SkinningBonesData");
        static readonly int SkinBonesId = Shader.PropertyToID("_SkinBones");
        static readonly int BonesCountId = Shader.PropertyToID("bonesCount");

        readonly ComputeShader _skinningComputeShader;
        readonly ComputeShader _prepareBonesShader;
        readonly int _prepareBonesKernel;
        readonly int _skinningKernel;
        readonly uint _xGroupSize;

        GraphicsBuffer _skinningBonesDataBuffer;
        GraphicsBuffer _skinBonesBuffer;

        UnsafeArray<RegisteredRenderer> _registeredRenderers;
        MemoryBookkeeper _memoryRegions;

        bool _frameInFlight;

        public float FillPercentage => (float) BonesCount / _memoryRegions.Capacity;
        uint BonesCount => _memoryRegions.LastBinStart;

        public BonesManager(ComputeShader skinningShader, ComputeShader prepareBonesShader) {
            var skinBonesCapacity = KandraRendererManager.FinalSkinBonesCapacity;
            var maxRenderers = KandraRendererManager.FinalRenderersCapacity;

            _prepareBonesShader = prepareBonesShader;
            _prepareBonesKernel = _prepareBonesShader.FindKernel("CSPrepareBones");
            _prepareBonesShader.GetKernelThreadGroupSizes(_prepareBonesKernel, out _xGroupSize, out _, out _);

            _skinningComputeShader = skinningShader;
            _skinningKernel = _skinningComputeShader.FindKernel("CSSkinning");

            _skinningBonesDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, skinBonesCapacity, sizeof(SkinningBoneData));
            _skinBonesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, skinBonesCapacity, sizeof(Bone));

            _registeredRenderers = new UnsafeArray<RegisteredRenderer>((uint)maxRenderers, ARAlloc.Persistent);
            _memoryRegions = new MemoryBookkeeper("Skin bones", (uint)skinBonesCapacity, maxRenderers/3, ARAlloc.Persistent);

            //EnsureBuffers();
        }

        public void Dispose() {
            _skinningBonesDataBuffer?.Dispose();
            _skinBonesBuffer?.Dispose();

            if (_registeredRenderers.IsCreated) {
                _registeredRenderers.Dispose();
                _memoryRegions.Dispose();
            }
        }

        public bool CanRegister(ushort[] boneIndices, out MemoryBookkeeper.MemoryRegion memoryDestination, ref string errorMessage) {
            var success = _memoryRegions.FindFreeRegion((uint)boneIndices.Length, out memoryDestination);
            if (!success) {
                errorMessage = BrokenKandraMessage.AppendMessageInfo(errorMessage, (uint)boneIndices.Length, _memoryRegions);
            }
            return success;
        }

        public void Register(uint slot, ushort[] boneIndices, in MemoryBookkeeper.MemoryRegion rendererBonesRegion, in MemoryBookkeeper.MemoryRegion rigMemory, in MemoryBookkeeper.MemoryRegion bindPosesMemory) {
            Asserts.AreEqual(bindPosesMemory.length, (uint)boneIndices.Length, $"For renderer {slot} bindPosesMemory length should be equal to boneIndices length");

            _memoryRegions.TakeFreeRegion(rendererBonesRegion);

            _registeredRenderers[slot] = new RegisteredRenderer {
                memory = rendererBonesRegion
            };

            UpdateBonesDataBuffer(boneIndices, rigMemory, bindPosesMemory, rendererBonesRegion);
        }

        public void Unregister(uint slot) {
            var renderer = _registeredRenderers[slot];
            _memoryRegions.Return(renderer.memory);
        }

        public void RigChanged(uint slot, ushort[] bones, MemoryBookkeeper.MemoryRegion rigRegion, MemoryBookkeeper.MemoryRegion meshRegionBindPosesMemory) {
            var renderer = _registeredRenderers[slot];
            UpdateBonesDataBuffer(bones, rigRegion, meshRegionBindPosesMemory, renderer.memory);
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            commandBuffer.SetComputeBufferParam(_prepareBonesShader, _prepareBonesKernel, SkinningBonesDataId, _skinningBonesDataBuffer);
            commandBuffer.SetComputeBufferParam(_prepareBonesShader, _prepareBonesKernel, SkinBonesId, _skinBonesBuffer);
            commandBuffer.SetComputeIntParam(_prepareBonesShader, BonesCountId, (int)BonesCount);

            commandBuffer.SetComputeBufferParam(_skinningComputeShader, _skinningKernel, BonesId, _skinBonesBuffer);
        }

        public void RunComputeShader(CommandBuffer commandBuffer) {
            var bonesCount = BonesCount;
            if (bonesCount > 0) {
                commandBuffer.DispatchCompute(_prepareBonesShader, _prepareBonesKernel, Mathf.CeilToInt((float)bonesCount / _xGroupSize), 1, 1);
            }
        }

        public bool TryGetBonesMemory(uint slot, out MemoryBookkeeper.MemoryRegion memory) {
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
            return memory.length * (ulong)(sizeof(SkinningBoneData));
        }

        void UpdateBonesDataBuffer(ushort[] boneIndices, MemoryBookkeeper.MemoryRegion rigMemory, MemoryBookkeeper.MemoryRegion bindPosesMemory, MemoryBookkeeper.MemoryRegion rendererBonesRegion) {
            var bonesData = new NativeArray<SkinningBoneData>(boneIndices.Length, ARAlloc.Temp);
            for (var i = 0; i < boneIndices.Length; i++) {
                bonesData[i] = new SkinningBoneData {
                    boneIndex = rigMemory.start + boneIndices[i],
                    bindPoseIndex = (uint)(bindPosesMemory.start + i)
                };
            }

            _skinningBonesDataBuffer.SetData(bonesData, 0, (int)rendererBonesRegion.start, bonesData.Length);
            bonesData.Dispose();
        }

        struct SkinningBoneData {
            public uint boneIndex;
            public uint bindPoseIndex;
        }

        struct RegisteredRenderer {
            public MemoryBookkeeper.MemoryRegion memory;
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 3;

            MemorySnapshotUtils.TakeSnapshot("SkinningBonesDataBuffer", _skinningBonesDataBuffer, BonesCount, memoryBuffer.Slice(0, 1));
            MemorySnapshotUtils.TakeSnapshot("SkinBonesBuffer", _skinBonesBuffer, BonesCount, memoryBuffer.Slice(1, 1));
            _memoryRegions.GetMemorySnapshot(memoryBuffer.Slice(2, 1));

            var selfSize = _registeredRenderers.Length * (ulong)sizeof(RegisteredRenderer);
            ownPlace.Span[0] = new MemorySnapshot(nameof(BonesManager), selfSize, selfSize, memoryBuffer[..childrenCount]);

            return childrenCount;
        }

        public readonly struct EditorAccess {
            readonly BonesManager _manager;

            public ref readonly MemoryBookkeeper SkinBonesMemory => ref _manager._memoryRegions;

            public EditorAccess(BonesManager manager) {
                _manager = manager;
            }

            public static EditorAccess Get() {
                return new EditorAccess(KandraRendererManager.Instance.BonesManager);
            }
        }
    }
}