using System;
using System.Collections.Generic;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.Kandra.Managers {
    [Il2CppEagerStaticClassConstruction]
    public class MeshBroker : IMemorySnapshotProvider {
        static readonly int IndicesBufferId = Shader.PropertyToID("_KandraIndicesBuffer");

        Dictionary<int, MeshData> _originalMeshes = new Dictionary<int, MeshData>(KandraRendererManager.FinalRenderersCapacity);
        Dictionary<uint, MeshData> _culledMeshes = new Dictionary<uint, MeshData>(KandraRendererManager.FinalRenderersCapacity);

        GraphicsBuffer _indicesBuffer;

        MemoryBookkeeper _indicesMemory;

        public GraphicsBufferHandle IndicesBufferHandle => _indicesBuffer.bufferHandle;

        public MeshBroker() {
            var indicesCapacity = KandraRendererManager.FinalIndicesCapacity;
            var maxRenderers = KandraRendererManager.FinalRenderersCapacity;

            _indicesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indicesCapacity, sizeof(ushort));
            _indicesMemory = new MemoryBookkeeper("Indices memo", (uint)indicesCapacity, maxRenderers/3, ARAlloc.Persistent);
        }

        public void Dispose() {
            _indicesBuffer?.Dispose();
            _indicesMemory.Dispose();

            _originalMeshes.Clear();
            _culledMeshes.Clear();
        }

        public KandraRenderingMesh TakeOriginalMesh(KandraMesh kandraMesh) {
            var hash = kandraMesh.GetHashCode();
            if (_originalMeshes.TryGetValue(hash, out var meshData)) {
                meshData.referenceCount++;
                _originalMeshes[hash] = meshData;
                return meshData.renderingMesh;
            }

            var indices = KandraRendererManager.Instance.StreamingManager.LoadIndicesData(kandraMesh);
            _indicesMemory.Take(indices.Length, out var indicesRegion);
            _indicesBuffer.SetData(indices.AsNativeArray(), 0, (int)indicesRegion.start, (int)indicesRegion.length);

            var submeshes = new UnsafeArray<SubmeshData>((uint)kandraMesh.submeshes.Length, ARAlloc.Persistent);
            for (var i = 0u; i < kandraMesh.submeshes.Length; i++) {
                var submesh = kandraMesh.submeshes[i];
                submeshes[i] = new SubmeshData {
                    indexStart = submesh.indexStart,
                    indexCount = submesh.indexCount,
                };
            }
            var renderingMesh = new KandraRenderingMesh {
                indexStart = indicesRegion.start,
                submeshes = submeshes,
            };

            _originalMeshes.Add(hash, new MeshData {
                referenceCount = 1,
                indicesMemory = indicesRegion,
                renderingMesh = renderingMesh,
            });

            return renderingMesh;
        }

        public void ReleaseOriginalMesh(KandraMesh kandraMesh) {
            var hash = kandraMesh.GetHashCode();
            if (_originalMeshes.TryGetValue(hash, out var meshData)) {
                meshData.referenceCount--;
                if (meshData.referenceCount == 0) {
                    _indicesMemory.Return(meshData.indicesMemory);
                    meshData.renderingMesh.Dispose();
                    _originalMeshes.Remove(hash);
                } else {
                    _originalMeshes[hash] = meshData;
                }
            }
        }

        public KandraRenderingMesh CreateCullableMesh(KandraMesh kandraMesh, UnsafeArray<ushort>.Span indices, UnsafeArray<SubmeshData> submeshes) {
            _indicesMemory.Take(indices.Length, out var indicesRegion);
            _indicesBuffer.SetData(indices.AsNativeArray(), 0, (int)indicesRegion.start, (int)indicesRegion.length);

            var renderingMesh = new KandraRenderingMesh {
                indexStart = indicesRegion.start,
                submeshes = submeshes,
            };

            var meshData = new MeshData {
                referenceCount = 1,
                indicesMemory = indicesRegion,
                renderingMesh = renderingMesh,
            };

            _culledMeshes.TryAdd(indicesRegion.start, meshData);
            return renderingMesh;
        }

        public void ReleaseCullableMesh(KandraMesh kandraMesh, KandraRenderingMesh renderingMesh) {
            if (_culledMeshes.TryGetValue(renderingMesh.indexStart, out var meshData)) {
                _indicesMemory.Return(meshData.indicesMemory);
                meshData.renderingMesh.Dispose();
                _culledMeshes.Remove(renderingMesh.indexStart);
            } else {
                Log.Critical?.Error($"Trying to release non-existing cullable mesh for {kandraMesh} with index {renderingMesh.indexStart}");
            }
        }

        public void EnsureBuffers(CommandBuffer commandBuffer) {
            commandBuffer.SetGlobalBuffer(IndicesBufferId, _indicesBuffer);
        }

        public struct MeshData {
            public int referenceCount;
            public MemoryBookkeeper.MemoryRegion indicesMemory;
            public KandraRenderingMesh renderingMesh;
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            MemorySnapshotUtils.TakeSnapshot("Indices buffer", _indicesBuffer, _indicesMemory.LastBinStart, memoryBuffer.Slice(0, 1));
            _indicesMemory.GetMemorySnapshot(memoryBuffer.Slice(1, 2));

            ownPlace.Span[0] = new(nameof(MeshBroker), 0, 0, memoryBuffer[..2]);

            return 2;
        }

        public readonly struct EditorAccess {
            readonly MeshBroker _meshBroker;

            public MemoryBookkeeper IndicesMemory => _meshBroker._indicesMemory;
            public Dictionary<int, MeshData> OriginalMeshes => _meshBroker._originalMeshes;
            public Dictionary<uint, MeshData> CulledMeshes => _meshBroker._culledMeshes;
            [UnityEngine.Scripting.Preserve] public GraphicsBuffer IndicesBuffer => _meshBroker._indicesBuffer;

            public EditorAccess(MeshBroker meshBroker) {
                _meshBroker = meshBroker;
            }

            public static EditorAccess Get() {
                return new EditorAccess(KandraRendererManager.Instance.MeshBroker);
            }
        }
    }
}