using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;

namespace Awaken.ECS.Components {
    public struct RotateTransformComponent : IComponentData, IWithDebugText {
        public float3 rotationAxis;
        public float rotationSpeed; //In radians per second
        
        public RotateTransformComponent(float3 rotationAxis, float rotationSpeed) {
            this.rotationAxis = rotationAxis;
            this.rotationSpeed = rotationSpeed;
        }

        public string DebugText =>
            $"rotation axis: {rotationAxis.ToString()}, rotation speed: {math.degrees(rotationSpeed)} degrees/sec";
    }
}