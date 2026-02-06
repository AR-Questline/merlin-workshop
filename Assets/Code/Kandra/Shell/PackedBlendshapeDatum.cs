using System;
using Unity.Mathematics;

namespace Awaken.Kandra.Data {
    public struct PackedBlendshapeDatum {
        public uint2 packedPositionDelta;
        public uint2 packedFinalNormalAndTangent;

        public PackedBlendshapeDatum(float3 positionDelta, float3 normalDelta, float3 tangentDelta,
            float3 originalNormal, float4 originalTangent) {
            throw new NotImplementedException();
        }
    }
}