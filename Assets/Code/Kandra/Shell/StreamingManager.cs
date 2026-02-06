using System;
using Awaken.Utility.LowLevel.Collections;
using UnityEngine;

namespace Awaken.Kandra.Managers {
    public class StreamingManager {
        public const string SubdirectoryName = "";
        public const string ArchiveFileName = "";

        public StreamingManager() {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public static string MeshDataPath(Mesh mesh) {
            throw new NotImplementedException();
        }

        public static string IndicesDataPath(Mesh mesh) {
            throw new NotImplementedException();
        }

        public static string KandraMeshName(Mesh mesh) {
            throw new NotImplementedException();
        }

        public string MeshDataPath(KandraMesh mesh) {
            throw new NotImplementedException();
        }

        public string IndicesDataPath(KandraMesh mesh) {
            throw new NotImplementedException();
        }

        public UnsafeArray<byte>.Span LoadMeshData(KandraMesh kandraMesh) {
            throw new NotImplementedException();
        }

        public UnsafeArray<ushort>.Span LoadIndicesData(KandraMesh kandraMesh) {
            throw new NotImplementedException();
        }

        public void UnloadMeshData(KandraMesh kandraMesh) {
            throw new NotImplementedException();
        }

        public void UnloadIndicesData(KandraMesh kandraMesh) {
            throw new NotImplementedException();
        }

        public void OnFrameEnd() {
            throw new NotImplementedException();
        }
    }
}