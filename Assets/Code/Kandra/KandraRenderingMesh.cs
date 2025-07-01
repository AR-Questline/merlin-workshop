using System;
using Awaken.Utility.LowLevel.Collections;
using Sirenix.OdinInspector;

namespace Awaken.Kandra {
    public struct KandraRenderingMesh {
        public static KandraRenderingMesh Invalid => new KandraRenderingMesh { indexStart = uint.MaxValue };

        [ShowInInspector] public uint indexStart;
        public UnsafeArray<SubmeshData> submeshes;

        [ShowInInspector] public readonly bool IsValid => submeshes.IsCreated;
        [ShowInInspector] readonly SubmeshData[] DebugSubmeshes => submeshes.IsCreated ? submeshes.ToManagedArray() : Array.Empty<SubmeshData>();

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
