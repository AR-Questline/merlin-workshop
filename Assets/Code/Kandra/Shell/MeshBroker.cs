using System;
using System.Collections.Generic;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    public class MeshBroker : IMemorySnapshotProvider {
        public GraphicsBufferHandle IndicesBufferHandle;
        private MemoryBookkeeper _indicesMemory;
        private Dictionary<int, MeshData> _originalMeshes;
        private Dictionary<uint, MeshData> _culledMeshes;
        private GraphicsBuffer _indicesBuffer;

        public MeshBroker() {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public KandraRenderingMesh TakeOriginalMesh(KandraMesh kandraMesh) {
            throw new NotImplementedException();
        }

        public void ReleaseOriginalMesh(KandraMesh kandraMesh) {
            throw new NotImplementedException();
        }

        public KandraRenderingMesh CreateCullableMesh(KandraMesh kandraMesh, UnsafeArray<ushort>.Span indices,
            UnsafeArray<SubmeshData> submeshes) {
            throw new NotImplementedException();
        }

        public void ReleaseCullableMesh(KandraMesh kandraMesh, KandraRenderingMesh renderingMesh) {
            throw new NotImplementedException();
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            throw new NotImplementedException();
        }

        public struct MeshData {
            public int referenceCount;
            public MemoryBookkeeper.MemoryRegion indicesMemory;
            public KandraRenderingMesh renderingMesh;
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            throw new NotImplementedException();
        }

        public readonly struct EditorAccess {
            readonly MeshBroker _meshBroker;

            public MemoryBookkeeper IndicesMemory => _meshBroker._indicesMemory;
            public Dictionary<int, MeshData> OriginalMeshes => _meshBroker._originalMeshes;
            public Dictionary<uint, MeshData> CulledMeshes => _meshBroker._culledMeshes;
            public GraphicsBuffer IndicesBuffer => _meshBroker._indicesBuffer;

            public EditorAccess(MeshBroker meshBroker) {
                throw new NotImplementedException();
            }

            public static EditorAccess Get() {
                throw new NotImplementedException();
            }
        }
    }
}