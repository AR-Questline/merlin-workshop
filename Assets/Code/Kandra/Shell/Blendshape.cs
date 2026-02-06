using System;
using Awaken.Utility.LowLevel.Collections;

namespace Awaken.Kandra.Data {
    public struct Blendshape {
        public UnsafeArray<PackedBlendshapeDatum>.Span data;

        public readonly uint Length => data.Length;

        public override int GetHashCode() {
            throw new NotImplementedException();
        }
    }
}