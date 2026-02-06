using System;
using Awaken.Utility.LowLevel.Collections;

namespace Awaken.Kandra {
    public struct KandraRenderingMesh {
        public static KandraRenderingMesh Invalid;
        public uint indexStart;
        public UnsafeArray<SubmeshData> submeshes;
        public readonly bool IsValid;

        public readonly uint IndexStart(uint submeshIndex) {
            throw new NotImplementedException();
        }

        public readonly uint IndexCount(uint submeshIndex) {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public override string ToString() {
            throw new NotImplementedException();
        }
    }
}