using System;
using Awaken.Utility.Maths;
using Unity.Mathematics;

namespace Awaken.Kandra.Data {
    [Serializable]
    public struct PackedBlendshapeDatum {
        public uint2 packedPositionDelta;
        public uint2 packedFinalNormalAndTangent;

        public PackedBlendshapeDatum(float3 positionDelta, float3 normalDelta, float3 tangentDelta, float3 originalNormal, float4 originalTangent) {
            var halfPosition = math.f32tof16(positionDelta);
            this.packedPositionDelta = default;
            this.packedPositionDelta.x = halfPosition.x | (halfPosition.y << 16);
            this.packedPositionDelta.y = halfPosition.z;

            var outputNormal = math.normalizesafe(normalDelta + originalNormal);
            var outputTangent = math.normalizesafe(tangentDelta + originalTangent.xyz);

            packedFinalNormalAndTangent = CompressionUtils.EncodeNormalAndTangent(outputNormal, outputTangent);
        }
    }
}