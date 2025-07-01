using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.DrakeRenderer {
    /// <summary>
    /// Component which is used as a source of transform data for drake entities in DynamicBuffer <see cref="DrakeVisualEntity"/>
    /// </summary>
    public struct DrakeVisualEntitiesTransform : IComponentData {
        public float3 position;
        public float scale;
        public quaternion rotation;

        public DrakeVisualEntitiesTransform(float3 position, quaternion rotation, float scale) {
            this.position = position;
            this.scale = scale;
            this.rotation = rotation;
        }

        public float3 Forward => math.rotate(rotation, math.forward());
        public float3 Right => math.rotate(rotation, math.right());
        public float4x4 Matrix => float4x4.TRS(position, rotation, scale);
    }
}