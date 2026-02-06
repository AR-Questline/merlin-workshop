using System;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public unsafe class MeshManager : IMemorySnapshotProvider {
        public float VerticesFillPercentage;
        public float IndicesFillPercentage;
        private MemoryBookkeeper _bindPosesMemory;
        private MemoryBookkeeper _verticesMemory;

        public MeshManager(ComputeShader skinningShader, ComputeShader prepareBonesShader) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public bool CanRegister(KandraMesh mesh, out MeshMemory memoryDestination, ref string errorMessage) {
            throw new NotImplementedException();
        }

        public void RegisterMesh(KandraMesh mesh, in MeshMemory memoryDestination) {
            throw new NotImplementedException();
        }

        public void UnregisterMesh(KandraMesh mesh) {
            throw new NotImplementedException();
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            throw new NotImplementedException();
        }

        public MeshMemory GetMeshMemory(KandraMesh mesh) {
            throw new NotImplementedException();
        }

        public bool TryGetMeshMemory(KandraMesh mesh, out MeshMemory memory) {
            throw new NotImplementedException();
        }

        public ulong GetMemoryUsageFor(KandraMesh mesh) {
            throw new NotImplementedException();
        }

        struct MeshData {
            public readonly MeshMemory memory;
            public int refCount;

            public MeshData(MeshMemory memory) {
                throw new NotImplementedException();
            }
        }

        public readonly struct MeshMemory : IEquatable<MeshMemory>, IComparable<MeshMemory> {
            public readonly MemoryBookkeeper.MemoryRegion bindPosesMemory;
            public readonly MemoryBookkeeper.MemoryRegion verticesMemory;

            public MeshMemory(MemoryBookkeeper.MemoryRegion bindPosesMemory,
                MemoryBookkeeper.MemoryRegion verticesMemory) {
                throw new NotImplementedException();
            }

            public bool Equals(MeshMemory other) {
                throw new NotImplementedException();
            }

            public int CompareTo(MeshMemory other) {
                throw new NotImplementedException();
            }

            public override string ToString() {
                throw new NotImplementedException();
            }
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }

        public readonly struct EditorAccess {
            readonly MeshManager _manager;

            public ref readonly MemoryBookkeeper BindPosesMemory => ref _manager._bindPosesMemory;
            public ref readonly MemoryBookkeeper VerticesMemory => ref _manager._verticesMemory;

            public EditorAccess(MeshManager manager) {
                throw new NotImplementedException();
            }

            public static EditorAccess Get() {
                throw new NotImplementedException();
            }

            public bool TryGetMeshMemory(KandraMesh mesh, out MeshMemory meshMemory) {
                throw new NotImplementedException();
            }
        }
    }
}