using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Flocks {
    public struct CurrentMovementVector : IComponentData {
        public float3 value;
        
        public CurrentMovementVector(float3 value) {
            this.value = value;
        }
    }
}