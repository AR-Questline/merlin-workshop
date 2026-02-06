using System;
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
        public bool makeSteeringSmoother;

        public MovementStaticParams(float maxAcceleration, float maxDeceleration,
            float maxDecelerationForReachRestPosition, float minSpeedForMovingToRestPosition,
            float toRestSteeringSpeedMult,
            SteeringParams avoidanceSteeringParams, float avoidanceSpeedMultiplierCurvePow,
            float avoidanceRotationSpeedAdditionWhenExceeding,
            bool makeSteeringSmoother) {
            throw new NotImplementedException();
        }
    }
}