using System;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.ECS.Flocks {
    [Serializable]
    public struct SteeringParams {
        [Tooltip("Max rotation speed (when damping multiplier = 1) in radians per second")]
        public float maxRotationSpeed;
        [Tooltip("Parameter p for function y = x^p which will be multiplier for maxRotationSpeed. X is normalized angle [0-180] between current direction and direction to target. Makes so that the closer entity is to the target rotation, the slower it rotates towards it, making entity to rotate fast to approximately correct direction and then slowly rotate to exactly correct direction. When p is 0-1: it is logarithmic (for the most of the time will be fast rotation), when > 1: exponential (for the most of the time will be slow rotation). The closer p is to 0, the faster entity will rotate towards exact rotation. The bigger p is, the quicker entity will decrease rotation speed to almost zero")]
        public float dampingCurvePow;
        [Tooltip("Parameter for clamping damping multiplier to range [mnDampingMultiplier, 1] so that rotation speed will not be zero")]
        public float dampingMultMinValue;

        public SteeringParams(float maxRotationSpeed, float dampingCurvePow, float dampingMultMinValue) {
            this.maxRotationSpeed = maxRotationSpeed;
            this.dampingCurvePow = dampingCurvePow;
            this.dampingMultMinValue = dampingMultMinValue;
        }

        public static SteeringParams Select(in SteeringParams valueIfFast, in SteeringParams valueIfTrue, bool condition) {
            var maxRotationSpeed = math.select(valueIfFast.maxRotationSpeed, valueIfTrue.maxRotationSpeed, condition);
            var dampingCurvePow = math.select(valueIfFast.dampingCurvePow, valueIfTrue.dampingCurvePow, condition);
            var minDampingMult = math.select(valueIfFast.dampingMultMinValue, valueIfTrue.dampingMultMinValue, condition);
            return new SteeringParams(maxRotationSpeed, dampingCurvePow, minDampingMult);
        }
    }
}