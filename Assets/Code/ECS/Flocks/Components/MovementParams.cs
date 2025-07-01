using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct MovementParams : IComponentData {
        public float movementSpeed;
        public float steeringSpeedMult;

        public MovementParams(float movementSpeed, float steeringSpeedMult) {
            this.movementSpeed = movementSpeed;
            this.steeringSpeedMult = steeringSpeedMult;
        }
    }
}