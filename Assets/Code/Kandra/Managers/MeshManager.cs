using System;
using Awaken.Kandra.Data;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public unsafe class MeshManager : IMemorySnapshotProvider {
        static readonly int BoneWeightsId = Shader.PropertyToID("_BoneWeights");
        static readonly int OriginalVerticesId = Shader.PropertyToID("_OriginalVertices");
        static readonly int BindPosesId = Shader.PropertyToID("_Bindposes");
        
        static readonly int GlobalAdditionalVerticesDataId = Shader.PropertyToID("_GlobalAdditionalVerticesData");
        static readonly int GlobalOriginalVerticesBufferId = Shader.PropertyToID("_GlobalOriginalVertices");

        readonly ComputeShader _prepareBonesShader;
        readonly int _prepareBonesKernel;
        readonly ComputeShader _skinningShader;
        readonly int _skinningKernel;

        GraphicsBuffer _bindPosesBuffer;
        GraphicsBuffer _originalVerticesBuffer;
        GraphicsBuffer _additionalVerticesDataBuffer;
        GraphicsBuffer _boneWeightsBuffer;

        MemoryBookkeeper _bindPosesMemory;
        MemoryBookkeeper _verticesMemory;
        UnsafeHashMap<int, MeshData> _meshes;
        
        public float VerticesFillPercentage => (float)_verticesMemory.LastBinStart / _verticesMemory.Capacity;
        public float IndicesFillPercentage => (float)_bindPosesMemory.LastBinStart / _bindPosesMemory.Capacity;

        public MeshManager(ComputeShader skinningShader, ComputeShader prepareBonesShader) {
            var verticesCapacity = KandraRendererManager.FinalUniqueVerticesCapacity;
            var bindposesCapacity = KandraRendererManager.FinalUniqueBindposesCapacity;
            var uniqueMeshesCapacity = KandraRendererManager.FinalUniqueMeshesCapacity;

            _prepareBonesShader = prepareBonesShader;
            _prepareBonesKernel = _prepareBonesShader.FindKernel("CSPrepareBones");
            _skinningShader = skinningShader;
            _skinningKernel = _skinningShader.FindKernel("CSSkinning");

            _bindPosesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bindposesCapacity, sizeof(float3x4));
            _originalVerticesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticesCapacity, sizeof(CompressedVertex));
            _additionalVerticesDataBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticesCapacity, sizeof(AdditionalVertexData));
            _boneWeightsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticesCapacity, sizeof(PackedBonesWeights));

            _bindPosesMemory = new MemoryBookkeeper("Bindposes memory", (uint)bindposesCapacity, uniqueMeshesCapacity / 3, ARAlloc.Persistent);
            _verticesMemory = new MemoryBookkeeper("Verts memory", (uint)verticesCapacity, uniqueMeshesCapacity / 3, ARAlloc.Persistent);
            _meshes = new UnsafeHashMap<int, MeshData>(uniqueMeshesCapacity, ARAlloc.Persistent);

            //EnsureBuffers();
        }

        public void Dispose() {
            _originalVerticesBuffer?.Dispose();
            _additionalVerticesDataBuffer?.Dispose();
            _bindPosesBuffer?.Dispose();
            _boneWeightsBuffer?.Dispose();
            _bindPosesMemory.Dispose();
            _verticesMemory.Dispose();
            _meshes.Dispose();
        }

        public bool CanRegister(KandraMesh mesh, out MeshMemory memoryDestination, ref string errorMessage) {
            var hash = mesh.GetHashCode();

            if (_meshes.TryGetValue(hash, out var data)) {
                memoryDestination = data.memory;
                return true;
            }
            memoryDestination = default;
            if (!_verticesMemory.FindFreeRegion(mesh.vertexCount, out var verticesRegion)) {
                errorMessage = BrokenKandraMessage.AppendMessageInfo(errorMessage, mesh.vertexCount, _verticesMemory);
                return false;
            }

            if (!_bindPosesMemory.FindFreeRegion(mesh.bindposesCount, out var bindPosesRegion)) {
                errorMessage = BrokenKandraMessage.AppendMessageInfo(errorMessage, mesh.bindposesCount, _bindPosesMemory);
                memoryDestination = new MeshMemory(default, verticesRegion);
                return false;
            }

            memoryDestination = new MeshMemory(bindPosesRegion, verticesRegion);
            return true;
        }

        public void RegisterMesh(KandraMesh mesh, in MeshMemory memoryDestination) {
            var hash = mesh.GetHashCode();

            if (_meshes.TryGetValue(hash, out var data)) {
                Asserts.AreEqual(data.memory, memoryDestination);
                data.refCount++;
                _meshes[hash] = data;
                return;
            }

            var meshData = mesh.ReadSerializedData(KandraRendererManager.Instance.StreamingManager.LoadMeshData(mesh));

            var verticesRegion = memoryDestination.verticesMemory;
            var bindPosesRegion = memoryDestination.bindPosesMemory;

            _verticesMemory.TakeFreeRegion(verticesRegion);
            _bindPosesMemory.TakeFreeRegion(bindPosesRegion);

            data = new MeshData(memoryDestination);

            // Copy mesh data to GPU
            _originalVerticesBuffer.SetData(meshData.vertices.AsNativeArray(), 0, (int)verticesRegion.start, (int)verticesRegion.length);
            _additionalVerticesDataBuffer.SetData(meshData.additionalData.AsNativeArray(), 0, (int)verticesRegion.start, (int)verticesRegion.length);
            _boneWeightsBuffer.SetData(meshData.boneWeights.AsNativeArray(), 0, (int)verticesRegion.start, (int)verticesRegion.length);
            _bindPosesBuffer.SetData(meshData.bindposes.AsNativeArray(), 0, (int)bindPosesRegion.start, (int)bindPosesRegion.length);

            _meshes[hash] = data;
        }

        public void UnregisterMesh(KandraMesh mesh) {
            var hash = mesh.GetHashCode();
            if (_meshes.TryGetValue(hash, out var data)) {
                data.refCount--;
                if (data.refCount == 0) {
                    _bindPosesMemory.Return(data.memory.bindPosesMemory);
                    _verticesMemory.Return(data.memory.verticesMemory);
                    _meshes.Remove(hash);
                } else {
                    _meshes[hash] = data;
                }
            }
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            commandBuffer.SetComputeBufferParam(_prepareBonesShader, _prepareBonesKernel, BindPosesId, _bindPosesBuffer);
            commandBuffer.SetComputeBufferParam(_skinningShader, _skinningKernel, BoneWeightsId, _boneWeightsBuffer);
            commandBuffer.SetComputeBufferParam(_skinningShader, _skinningKernel, OriginalVerticesId, _originalVerticesBuffer);

            commandBuffer.SetGlobalBuffer(GlobalOriginalVerticesBufferId, _originalVerticesBuffer);
            commandBuffer.SetGlobalBuffer(GlobalAdditionalVerticesDataId, _additionalVerticesDataBuffer);
        }

        public MeshMemory GetMeshMemory(KandraMesh mesh) {
            var hash = mesh.GetHashCode();
            return _meshes[hash].memory;
        }

        public bool TryGetMeshMemory(KandraMesh mesh, out MeshMemory memory) {
            var hash = mesh.GetHashCode();
            if (_meshes.TryGetValue(hash, out var data)) {
                memory = data.memory;
                return true;
            }

            memory = default;
            return false;
        }

        public ulong GetMemoryUsageFor(KandraMesh mesh) {
            var hash = mesh.GetHashCode();
            if (_meshes.TryGetValue(hash, out var data)) {
                var memory = data.memory;
                var size = 0ul;

                size += memory.verticesMemory.length * (ulong)sizeof(CompressedVertex);
                size += memory.verticesMemory.length * (ulong)sizeof(AdditionalVertexData);
                size += memory.verticesMemory.length * (ulong)sizeof(PackedBonesWeights);
                size += memory.bindPosesMemory.length * (ulong)sizeof(float3x4);

                return size;
            }

            return 0;
        }

        struct MeshData {
            public readonly MeshMemory memory;
            public int refCount;

            public MeshData(MeshMemory memory) {
                this.memory = memory;
                refCount = 1;
            }
        }

        public readonly struct MeshMemory : IEquatable<MeshMemory>, IComparable<MeshMemory> {
            public readonly MemoryBookkeeper.MemoryRegion bindPosesMemory;
            public readonly MemoryBookkeeper.MemoryRegion verticesMemory;

            public MeshMemory(MemoryBookkeeper.MemoryRegion bindPosesMemory,
                MemoryBookkeeper.MemoryRegion verticesMemory) {
                this.bindPosesMemory = bindPosesMemory;
                this.verticesMemory = verticesMemory;
            }

            public bool Equals(MeshMemory other) {
                return bindPosesMemory.Equals(other.bindPosesMemory) && verticesMemory.Equals(other.verticesMemory);
            }

            public int CompareTo(MeshMemory other) {
                if (bindPosesMemory.Equals(other.bindPosesMemory)) {
                    return verticesMemory.CompareTo(other.verticesMemory);
                }
                return bindPosesMemory.CompareTo(other.bindPosesMemory);
            }

            public override string ToString() {
                return $"BindPoses: {bindPosesMemory}, Vertices: {verticesMemory}";
            }
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var childrenCount = 6;

            MemorySnapshotUtils.TakeSnapshot("BindPosesBuffer", _bindPosesBuffer, _bindPosesMemory.LastBinStart, memoryBuffer.Slice(0, 1));
            MemorySnapshotUtils.TakeSnapshot("OriginalVerticesBuffer", _originalVerticesBuffer, _verticesMemory.LastBinStart, memoryBuffer.Slice(1, 1));
            MemorySnapshotUtils.TakeSnapshot("BoneWeightsBuffer", _boneWeightsBuffer, _verticesMemory.LastBinStart, memoryBuffer.Slice(2, 1));
            MemorySnapshotUtils.TakeSnapshot("MeshesMap", _meshes, memoryBuffer.Slice(3, 1));
            _bindPosesMemory.GetMemorySnapshot(memoryBuffer.Slice(4, 1));
            _verticesMemory.GetMemorySnapshot(memoryBuffer.Slice(5, 1));

            ownPlace.Span[0] = new(nameof(MeshManager), 0, 0, memoryBuffer[..childrenCount]);

            return childrenCount;
        }

        public readonly struct EditorAccess {
            readonly MeshManager _manager;

            public ref readonly MemoryBookkeeper BindPosesMemory => ref _manager._bindPosesMemory;
            public ref readonly MemoryBookkeeper VerticesMemory => ref _manager._verticesMemory;

            public EditorAccess(MeshManager manager) {
                _manager = manager;
            }

            public static EditorAccess Get() {
                return new EditorAccess(KandraRendererManager.Instance.MeshManager);
            }

            public bool TryGetMeshMemory(KandraMesh mesh, out MeshMemory meshMemory) {
                var hash = mesh.GetHashCode();
                if (_manager._meshes.TryGetValue(hash, out var data)) {
                    meshMemory = data.memory;
                    return true;
                }

                meshMemory = default;
                return false;
            }
        }
    }
}