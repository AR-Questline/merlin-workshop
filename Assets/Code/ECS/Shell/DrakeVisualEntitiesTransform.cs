using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.DrakeRenderer {
    public struct DrakeVisualEntitiesTransform : IComponentData {
        public float3 position;
        public float scale;
        public quaternion rotation;

        public DrakeVisualEntitiesTransform(float3 position, quaternion rotation, float scale) {
            throw new NotImplementedException();
        }

        public float3 Forward => throw new NotImplementedException();
        public float3 Right => throw new NotImplementedException();
        public float4x4 Matrix => throw new NotImplementedException();
    }
}