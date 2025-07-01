using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    public struct FlockGroupTargetPosition : IComponentData {
        public float3 value;
        public FlockGroupTargetPosition(float3 value) {
            this.value = value;
        }
    }
}