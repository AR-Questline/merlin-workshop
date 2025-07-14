using System.IO;
using Awaken.Utility.Archives;
using Awaken.Utility.Assets.Modding;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using Awaken.Utility.Debugging;

namespace Awaken.Kandra.Managers {
    public class StreamingManager {
        public const string SubdirectoryName = "Kandra";
        public const string ArchiveFileName = "kandra.arch";
        const int PreAllocCount = 16;
        static string EditorBasePath => Path.Combine(Application.streamingAssetsPath, SubdirectoryName);

        UnsafeHashMap<int, UnsafeArray<byte>> _meshesCache;
        UnsafeHashMap<int, UnsafeArray<ushort>> _indicesCache;

        string _basePath;

        public StreamingManager() {
            _meshesCache = new UnsafeHashMap<int, UnsafeArray<byte>>(PreAllocCount, ARAlloc.Persistent);
            _indicesCache = new UnsafeHashMap<int, UnsafeArray<ushort>>(PreAllocCount, ARAlloc.Persistent);

            _basePath = EditorBasePath;
            var success = ArchiveUtils.TryMountAndAdjustPath("Kandra", SubdirectoryName, ArchiveFileName, ref _basePath);
            if (!success) {
                Log.Critical?.Error($"Skills merged archive not found at {Path.Combine(Application.streamingAssetsPath, SubdirectoryName, ArchiveFileName)}");
            }
        }

        public void Dispose() {
            OnFrameEnd();
            _meshesCache.Dispose();
            _indicesCache.Dispose();
        }

        public static string MeshDataPath(Mesh mesh) {
#if UNITY_EDITOR
            if (!Directory.Exists(EditorBasePath)) {
                Directory.CreateDirectory(EditorBasePath);
            }
            var meshFullName = $"{KandraMeshName(mesh)}.mdkandra";
            return Path.Combine(EditorBasePath, meshFullName);
#else
            Log.Critical?.Error("Mesh data path is not available in build");
            return null;
#endif
        }

        public static string IndicesDataPath(Mesh mesh) {
#if UNITY_EDITOR
            if (!Directory.Exists(EditorBasePath)) {
                Directory.CreateDirectory(EditorBasePath);
            }
            var meshFullName = $"{KandraMeshName(mesh)}.ixkandra";
            return Path.Combine(EditorBasePath, meshFullName);
#else
            Log.Critical?.Error("Mesh data path is not available in build");
            return null;
#endif
        }

        public static string KandraMeshName(Mesh mesh) {
#if UNITY_EDITOR
            var meshPath = UnityEditor.AssetDatabase.GetAssetPath(mesh);
            var fbx = UnityEditor.AssetDatabase.LoadMainAssetAtPath(meshPath);
            return $"{fbx.name}_{mesh.name}";
#else
            Log.Critical?.Error("Mesh data path is not available in build");
            return null;
#endif
        }

        public string MeshDataPath(KandraMesh mesh) {
            if (string.IsNullOrEmpty(mesh.modDirectory)) {
                return Path.Combine(_basePath, mesh.name + ".mdkandra");
            } else {
                return Path.Combine(ModManager.ModDirectoryPath, mesh.modDirectory, SubdirectoryName, mesh.name + ".mdkandra");
            }
        }

        public string IndicesDataPath(KandraMesh mesh) {
            if (string.IsNullOrEmpty(mesh.modDirectory)) {
                return Path.Combine(_basePath, mesh.name + ".ixkandra");
            } else {
                return Path.Combine(ModManager.ModDirectoryPath, mesh.modDirectory, SubdirectoryName, mesh.name + ".ixkandra");
            }
        }

        public UnsafeArray<byte>.Span LoadMeshData(KandraMesh kandraMesh) {
            var hash = kandraMesh.GetHashCode();
            if (!_meshesCache.TryGetValue(hash, out var meshData)) {
                // Need to be TempJob as in frame there is a point where Unity already freed the memory but frame wasn't advanced to next yet
                meshData = Buffer<byte>(MeshDataPath(kandraMesh), ARAlloc.TempJob);
                _meshesCache.TryAdd(hash, meshData);
            }
            return meshData;
        }

        public UnsafeArray<ushort>.Span LoadIndicesData(KandraMesh kandraMesh) {
            var hash = kandraMesh.GetHashCode();
            if (!_indicesCache.TryGetValue(hash, out var indices)) {
                // Need to be TempJob as in frame there is a point where Unity already freed the memory but frame wasn't advanced to next yet
                indices = Buffer<ushort>(IndicesDataPath(kandraMesh), 0, kandraMesh.indicesCount, ARAlloc.TempJob);
                _indicesCache.TryAdd(hash, indices);
            }
            return indices;
        }

        public void UnloadMeshData(KandraMesh kandraMesh) {
            var hash = kandraMesh.GetHashCode();
            if (_meshesCache.TryGetValue(hash, out var meshData)) {
                meshData.Dispose();
                _meshesCache.Remove(hash);
            }
        }

        public void UnloadIndicesData(KandraMesh kandraMesh) {
            var hash = kandraMesh.GetHashCode();
            if (_indicesCache.TryGetValue(hash, out var indices)) {
                indices.Dispose();
                _indicesCache.Remove(hash);
            }
        }

        public void OnFrameEnd() {
            foreach (var meshes in _meshesCache) {
                meshes.Value.Dispose();
            }
            _meshesCache.Clear();
            foreach (var indices in _indicesCache) {
                indices.Value.Dispose();
            }
            _indicesCache.Clear();
        }

        static unsafe UnsafeArray<T> Buffer<T>(string filepath, Allocator allocator) where T : unmanaged {
            var fileInfo = default(FileInfoResult);
            AsyncReadManager.GetFileInfo(filepath, &fileInfo).JobHandle.Complete();

            var cellCount = (uint)(fileInfo.FileSize / UnsafeUtility.SizeOf<T>());
            return Buffer<T>(filepath, 0, cellCount, allocator);
        }

        static unsafe UnsafeArray<T> Buffer<T>(string filepath, long offset, uint count, Allocator allocator) where T : unmanaged {
            var buffer = new UnsafeArray<T>(count, allocator, NativeArrayOptions.UninitializedMemory);
            var readCommand = new ReadCommand {
                Offset = offset,
                Size = UnsafeUtility.SizeOf<T>() * count,
                Buffer = buffer.Ptr,
            };
            var readHandle = AsyncReadManager.Read(filepath, &readCommand, 1);
            AsyncReadManager.CloseCachedFileAsync(filepath, readHandle.JobHandle).Complete();
            return buffer;
        }
    }
}