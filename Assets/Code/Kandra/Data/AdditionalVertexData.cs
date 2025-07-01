using System;
using Unity.Mathematics;

namespace Awaken.Kandra.Data {
    [Serializable]
    public struct AdditionalVertexData {
        public uint uv;
        public float tangentW;

        public float2 UV {
            get {
                var uvX = (uv & 0xFFFF);
                var uvY = (uv >> 16);
                return math.f16tof32(new uint2(uvX, uvY));
            }
        }

        public AdditionalVertexData(float2 uv, float tangentW) {
            var halfUv = math.f32tof16(uv);
            this.uv = halfUv.x | (halfUv.y << 16);
            this.tangentW = tangentW;
        }
    }
}