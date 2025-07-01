using System;
using Awaken.Kandra.Data;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UniversalProfiling;

namespace Awaken.Kandra.Managers {
    [BurstCompile]
    public unsafe class BlendshapesManager : IMemorySnapshotProvider {
        static readonly UniversalProfilerMarker UpdateBlendshapesMarker = new UniversalProfilerMarker("KandraRenderer.UpdateBlendshapes");

        static readonly int BlendshapeDataId = Shader.PropertyToID("_BlendshapeData");
        static readonly int BlendshapesDeltasId = Shader.PropertyToID("_BlendshapesDeltas");
        static readonly int BlendshapeIndicesAndWeightsId = Shader.PropertyToID("_BlendshapeIndicesAndWeights");

        readonly ComputeShader _skinningShader;
        readonly int _skinningKernel;

        GraphicsBuffer _blendshapeDataBuffer;
        GraphicsBuffer _blendshapesDeltasBuffer;
        GraphicsBuffer _blendshapeIndicesAndWeightsBuffer;

        UnsafeArray<UnsafeArray<float>.Span> _weights;
        UnsafeArray<UnsafeArray<uint>> _indices;

        UnsafeHashMap<int, BlendshapesData> _blendshapes;
        MemoryBookkeeper _blendshapesMemory;
        
        public float FillPercentage => (float)_blendshapesMemory.LastBinStart / _blendshapesMemory.Capacity;

        public BlendshapesManager(ComputeShader skinningShader) {
            var renderersCapacity = KandraRendererManager.FinalRenderersCapacity;
            var blendshapesCapacity = KandraRendererManager.FinalBlendshapesCapacity;
            var blendshapesDeltasCapacity = KandraRendererManager.FinalBlendshapesDeltasCapacity;

            _skinningShader = skinningShader;
            _skinningKernel = _skinningShader.FindKernel("CSSkinning");

            _blendshapeDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, renderersCapacity, sizeof(BlendshapesInstanceDatum));

            _blendshapesDeltasBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, blendshapesDeltasCapacity, sizeof(PackedBlendshapeDatum));
            _blendshapeIndicesAndWeightsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, blendshapesCapacity, sizeof(BlendshapeIndexAndWeight));

            _blendshapes = new UnsafeHashMap<int, BlendshapesData>(blendshapesCapacity, ARAlloc.Persistent);
            _blendshapesMemory = new MemoryBookkeeper("Blendshape deltas", (uint)blendshapesDeltasCapacity, renderersCapacity/3, ARAlloc.Persistent);

            _weights = new UnsafeArray<UnsafeArray<float>.Span>((uint)renderersCapacity, ARAlloc.Persistent);
            _indices = new UnsafeArray<UnsafeArray<uint>>((uint)renderersCapacity, ARAlloc.Persistent);

            //EnsureBuffers();
        }

        public void Dispose() {
            _blendshapeDataBuffer?.Dispose();
            _blendshapesDeltasBuffer?.Dispose();
            _blendshapeIndicesAndWeightsBuffer?.Dispose();

            var blendshapesValues = _blendshapes.GetValueArray(ARAlloc.Temp);
            for (var i = 0; i < blendshapesValues.Length; i++) {
                blendshapesValues[i].Dispose();
            }
            blendshapesValues.Dispose();
            _blendshapes.Dispose();
            _blendshapesMemory.Dispose();
            _weights.Dispose();
            for (var i = 0u; i < _indices.Length; i++) {
                _indices[i].DisposeIfCreated();
            }
            _indices.Dispose();
        }

        public bool CanRegister(KandraMesh mesh, out UnsafeArray<MemoryBookkeeper.MemoryRegion> memoryDestinations, ref string errorMessage) {
            var hash = mesh.GetHashCode();

            if (_blendshapes.TryGetValue(hash, out var data)) {
                memoryDestinations = new UnsafeArray<MemoryBookkeeper.MemoryRegion>(data.blendshapesMemory, ARAlloc.Temp);
                return true;
            }

            var blendshapesCount = mesh.blendshapesNames.Length;
            memoryDestinations = new UnsafeArray<MemoryBookkeeper.MemoryRegion>((uint)blendshapesCount, ARAlloc.Temp);
            var success = true;
            for (var i = 0u; success && i < blendshapesCount; ++i) {
                if (!_blendshapesMemory.FindFreeRegion(mesh.vertexCount, out memoryDestinations[i])) {
                    errorMessage = BrokenKandraMessage.AppendMessageInfo(errorMessage, mesh.vertexCount, _blendshapesMemory);
                    success = false;
                }
                if (success) {
                    _blendshapesMemory.TakeFreeRegion(memoryDestinations[i]);
                }
            }
            for (var i = 0u; i < blendshapesCount; ++i) {
                if (memoryDestinations[i].IsValid) {
                    _blendshapesMemory.Return(memoryDestinations[i]);
                }
            }
            return success;
        }

        public void Register(uint slot, KandraMesh mesh, UnsafeArray<float>.Span rendererWeights, UnsafeArray<MemoryBookkeeper.MemoryRegion> memoryDestinations) {
            var hash = mesh.GetHashCode();
            var indices = rendererWeights.Length == 0 ? default : new UnsafeArray<uint>(rendererWeights.Length, ARAlloc.Persistent);

            if (_blendshapes.TryGetValue(hash, out var dataArray)) {
                for (var i = 0u; i < dataArray.Length; ++i) {
                    Asserts.AreEqual(memoryDestinations[i], dataArray.blendshapesMemory[i]);
                    indices[i] = dataArray.blendshapesMemory[i].start;
                }
                dataArray.refCount++;
                _blendshapes[hash] = dataArray;
            } else {
                var blendshapes = mesh.ReadBlendshapesData(KandraRendererManager.Instance.StreamingManager.LoadMeshData(mesh), ARAlloc.Temp);

                var blendshapesData = new UnsafeArray<MemoryBookkeeper.MemoryRegion>(blendshapes.Length, ARAlloc.Persistent);
                for (var i = 0u; i < blendshapes.Length; ++i) {
                    var blendshape = blendshapes[i];

                    var blendshapeMemory = memoryDestinations[i];
                    _blendshapesMemory.TakeFreeRegion(blendshapeMemory);
                    _blendshapesDeltasBuffer.SetData(blendshape.data.AsNativeArray(), 0, (int)blendshapeMemory.start, (int)blendshapeMemory.length);

                    blendshapesData[i] = blendshapeMemory;
                    indices[i] = blendshapeMemory.start;
                }
                dataArray = new BlendshapesData(blendshapesData);
                _blendshapes.TryAdd(hash, dataArray);

                blendshapes.Dispose();
            }

            _indices[slot] = indices;
            _weights[slot] = rendererWeights;
        }

        public void Unregister(uint slot, KandraMesh mesh) {
            var hash = mesh.GetHashCode();
            if (_blendshapes.TryGetValue(hash, out var dataArray)) {
                dataArray.refCount--;
                if (dataArray.refCount == 0) {
                    for (var i = 0u; i < dataArray.Length; i++) {
                        _blendshapesMemory.Return(dataArray.blendshapesMemory[i]);
                    }
                    dataArray.Dispose();
                    _blendshapes.Remove(hash);
                } else {
                    _blendshapes[hash] = dataArray;
                }
            }

            _indices[slot].DisposeIfCreated();
            _indices[slot] = default;
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            commandBuffer.SetComputeBufferParam(_skinningShader, _skinningKernel, BlendshapeDataId, _blendshapeDataBuffer);
            commandBuffer.SetComputeBufferParam(_skinningShader, _skinningKernel, BlendshapesDeltasId, _blendshapesDeltasBuffer);
            commandBuffer.SetComputeBufferParam(_skinningShader, _skinningKernel, BlendshapeIndicesAndWeightsId, _blendshapeIndicesAndWeightsBuffer);
        }

        public void UpdateBlendshapes(UnsafeBitmask takenSlots) {
            UpdateBlendshapesMarker.Begin();
            var nonZeroIndicesAndWeights = new NativeList<BlendshapeIndexAndWeight>(_weights.LengthInt, ARAlloc.Temp);
            var instancesData = new UnsafeArray<BlendshapesInstanceDatum>(_weights.Length, ARAlloc.Temp);

            CollectActiveBlendshapesData(_weights, _indices, takenSlots, ref nonZeroIndicesAndWeights, ref instancesData);

            _blendshapeIndicesAndWeightsBuffer.SetData(nonZeroIndicesAndWeights.AsArray());
            _blendshapeDataBuffer.SetData(instancesData.AsNativeArray());

            nonZeroIndicesAndWeights.Dispose();
            instancesData.Dispose();
            UpdateBlendshapesMarker.End();
        }

        public bool TryGetBlendshapesData(KandraMesh mesh, out UnsafeArray<MemoryBookkeeper.MemoryRegion> data) {
            if (_blendshapes.TryGetValue(mesh.GetHashCode(), out var blendshapesData)) {
                data = blendshapesData.blendshapesMemory;
                return true;
            }

            data = default;
            return false;
        }

        public ulong GetMemoryUsageFor(KandraMesh mesh) {
            if (_blendshapes.TryGetValue(mesh.GetHashCode(), out var blendshapesData)) {
                var data = blendshapesData.blendshapesMemory;
                var vertxCount = mesh.vertexCount;
                ulong total = data.Length * vertxCount * (ulong)sizeof(PackedBlendshapeDatum) +
                              (ulong)sizeof(BlendshapesInstanceDatum);
                return total;
            }

            return 0;
        }

        [BurstCompile]
        static void CollectActiveBlendshapesData(in UnsafeArray<UnsafeArray<float>.Span> weights, in UnsafeArray<UnsafeArray<uint>> indices,
            in UnsafeBitmask takenSlots, ref NativeList<BlendshapeIndexAndWeight> nonZeroIndicesAndWeights, ref UnsafeArray<BlendshapesInstanceDatum> instancesData) {
            foreach (var i in takenSlots.EnumerateOnes()) {
                var subWeights = weights[i];
                var start = (uint)nonZeroIndicesAndWeights.Length;

                for (var j = 0u; j < subWeights.Length; j++) {
                    if (math.abs(subWeights[j]) > 0.001f) {
                        var index = indices[i][j];
                        var weight = subWeights[j];
                        var blendshapeIndexAndWeight = new BlendshapeIndexAndWeight {
                            index = index,
                            weight = weight
                        };
                        nonZeroIndicesAndWeights.Add(blendshapeIndexAndWeight);
                    }
                }

                var instanceData = new BlendshapesInstanceDatum {
                    startAndLengthOfWeights = start | (uint)((nonZeroIndicesAndWeights.Length - start) << 16),
                };
                instancesData[i] = instanceData;
            }
        }

        struct BlendshapesInstanceDatum {
            public uint startAndLengthOfWeights;
        }

        struct BlendshapeIndexAndWeight {
            public uint index;
            public float weight;
        }

        public struct BlendshapesData {
            public readonly UnsafeArray<MemoryBookkeeper.MemoryRegion> blendshapesMemory;
            public int refCount;

            public readonly uint Length => blendshapesMemory.Length;

            public BlendshapesData(UnsafeArray<MemoryBookkeeper.MemoryRegion> memory) {
                blendshapesMemory = memory;
                refCount = 1;
            }

            public void Dispose() {
                blendshapesMemory.Dispose();
            }
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 5;

            var registeredRenderers = KandraRendererManager.Instance.RegisteredRenderers;
            MemorySnapshotUtils.TakeSnapshot("BlendshapeDataBuffer", _blendshapeDataBuffer, registeredRenderers, memoryBuffer.Slice(0, 1));
            MemorySnapshotUtils.TakeSnapshot("BlendshapesDeltasBuffer", _blendshapesDeltasBuffer, _blendshapesMemory.LastBinStart, memoryBuffer.Slice(1, 1));
            MemorySnapshotUtils.TakeSnapshot("IndicesAndWeightsBuffer", _blendshapeIndicesAndWeightsBuffer, registeredRenderers, memoryBuffer.Slice(2, 1));
            _blendshapesMemory.GetMemorySnapshot(memoryBuffer.Slice(3, 1));
            MemorySnapshotUtils.TakeSnapshot("Blendshapes map", _blendshapes, memoryBuffer.Slice(4, 1));

            var selfSize = _weights.Length * (ulong)IntPtr.Size * 2;
            var usedSize = 0ul;

            for (var i = 0u; i < _weights.Length; ++i) {
                if (_weights[i].IsValid) {
                    var weightsSize = (ulong)(_weights[i].Length * sizeof(float));
                    var indicesSize = (ulong)(_indices[i].Length * sizeof(uint));
                    selfSize += weightsSize + indicesSize;
                    usedSize += weightsSize + indicesSize + ((ulong)IntPtr.Size * 2);
                }
            }

            ownPlace.Span[0] = new(nameof(BlendshapesManager), selfSize, usedSize, memoryBuffer[..childrenCount]);

            return childrenCount;
        }

        public readonly struct EditorAccess {
            readonly BlendshapesManager _manager;

            public ref readonly MemoryBookkeeper BlendshapesMemory => ref _manager._blendshapesMemory;
            public ref readonly UnsafeArray<UnsafeArray<uint>> Indices => ref _manager._indices;
            public ref readonly UnsafeArray<UnsafeArray<float>.Span> Weights => ref _manager._weights;
            public ref readonly UnsafeHashMap<int, BlendshapesData> Blendshapes => ref _manager._blendshapes;

            public EditorAccess(BlendshapesManager manager) {
                _manager = manager;
            }

            public static EditorAccess Get() => new EditorAccess(KandraRendererManager.Instance.BlendshapesManager);
        }
    }
}