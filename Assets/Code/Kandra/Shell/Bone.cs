using System;
using Unity.Mathematics;

namespace Awaken.Kandra.Data {
    public struct Bone {
        public float3x4 boneTransform;

        public Bone(float3x4 boneTransform) {
            throw new NotImplementedException();
        }

        public Bone(float4x4 boneTransform) {
            throw new NotImplementedException();
        }
    }
}