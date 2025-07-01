using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Rendering;

namespace Unity.Entities.Graphics {
    public unsafe struct RendererRange : ISharedComponentData, IEquatable<RendererRange> {
        [NativeDisableUnsafePtrRestriction] public MaterialMeshInfo* MaterialMeshInfos;

        public bool Equals(RendererRange other) {
            return MaterialMeshInfos == other.MaterialMeshInfos;
        }

        public override bool Equals(object obj) {
            return obj is RendererRange other && Equals(other);
        }

        public override int GetHashCode() {
            return unchecked((int)(long)MaterialMeshInfos);
        }
    }
}