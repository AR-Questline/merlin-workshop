using System;
using Unity.Mathematics;

namespace Awaken.Kandra.Data {
    public struct AdditionalVertexData {
        public uint uv;
        public float tangentW;

        public float2 UV;

        public AdditionalVertexData(float2 uv, float tangentW) {
            throw new NotImplementedException();
        }
    }
}