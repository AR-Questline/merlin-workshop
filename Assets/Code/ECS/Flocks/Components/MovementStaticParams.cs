using Unity.Entities;

namespace Awaken.ECS.Flocks {
    public struct MovementStaticParams : IComponentData {
        public float maxAcceleration;
        public float maxDeceleration;
        public float maxDecelerationForReachRestPosition;
        public float minSpeedForMovingToRestPosition;
        public float toRestSteeringSpeedMult;
        public SteeringParams avoidanceSteeringParams;
        public float avoidanceSpeedMultiplierCurvePow;
        public float avoidanceRotationSpeedAdditionWhenExceeding;

        public MovementStaticParams(float maxAcceleration, float maxDeceleration, float maxDecelerationForReachRestPosition, float minSpeedForMovingToRestPosition, float toRestSteeringSpeedMult,
            SteeringParams avoidanceSteeringParams, float avoidanceSpeedMultiplierCurvePow, float avoidanceRotationSpeedAdditionWhenExceeding) {
            this.maxAcceleration = maxAcceleration;
            this.maxDeceleration = maxDeceleration;
            this.maxDecelerationForReachRestPosition = maxDecelerationForReachRestPosition;
            this.minSpeedForMovingToRestPosition = minSpeedForMovingToRestPosition;
            this.toRestSteeringSpeedMult = toRestSteeringSpeedMult;
            this.avoidanceSteeringParams = avoidanceSteeringParams;
            this.avoidanceSpeedMultiplierCurvePow = avoidanceSpeedMultiplierCurvePow;
            this.avoidanceRotationSpeedAdditionWhenExceeding = avoidanceRotationSpeedAdditionWhenExceeding;
        }
    }
}