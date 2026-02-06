using System;

namespace Awaken.ECS.Flocks {
    [Serializable]
    public struct SteeringParams {
        public float maxRotationSpeed;
        public float dampingCurvePow;
        public float dampingMultMinValue;

        public SteeringParams(float maxRotationSpeed, float dampingCurvePow, float dampingMultMinValue) {
            throw new NotImplementedException();
        }

        public static SteeringParams Select(in SteeringParams valueIfFast, in SteeringParams valueIfTrue,
            bool condition) {
            throw new NotImplementedException();
        }
    }
}