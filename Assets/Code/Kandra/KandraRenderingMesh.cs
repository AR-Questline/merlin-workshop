using Awaken.Utility.LowLevel.Collections;

namespace Awaken.Kandra {
    public struct KandraRenderingMesh {
        public static KandraRenderingMesh Invalid => new KandraRenderingMesh { indexStart = uint.MaxValue };

        public uint indexStart;
        public UnsafeArray<SubmeshData> submeshes;

        public readonly bool IsValid => submeshes.IsCreated;

        public readonly uint IndexStart(uint submeshIndex) {
            return submeshes[submeshIndex].indexStart + indexStart;
        }

        public readonly uint IndexCount(uint submeshIndex) {
            return submeshes[submeshIndex].indexCount;
        }

        public void Dispose() {
            submeshes.Dispose();
        }

        public override string ToString() {
            var submeshesDebug = string.Join(", ", submeshes.AsNativeArray());
            return $"KandraRenderingMesh: {indexStart} - {submeshesDebug}";
        }
    }
}
