using Unity.Entities;
using Unity.Mathematics;

namespace Awaken.ECS.Components {
    public readonly struct LinkedTransformLocalToWorldOffsetComponent : IComponentData {
        public readonly float4x4 offsetMatrix;

        public LinkedTransformLocalToWorldOffsetComponent(float4x4 offsetMatrix) {
            this.offsetMatrix = offsetMatrix;
        }
    }
}
