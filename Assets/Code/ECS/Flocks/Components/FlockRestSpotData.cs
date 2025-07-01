using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    public struct FlockRestSpotData : IComponentData {
        public Entity flockGroupEntity;
        public float3 position;
        public float radius;
        public float2 autoCatchDelayMinMax;
        public float2 autoDismountDelayMinMax;

        public FlockRestSpotData(Entity flockGroupEntity, float3 position, float radius, float2 autoCatchDelayMinMax, float2 autoDismountDelayMinMax) {
            this.flockGroupEntity = flockGroupEntity;
            this.position = position;
            this.radius = radius;
            this.autoCatchDelayMinMax = autoCatchDelayMinMax;
            this.autoDismountDelayMinMax = autoDismountDelayMinMax;
        }
    }
}