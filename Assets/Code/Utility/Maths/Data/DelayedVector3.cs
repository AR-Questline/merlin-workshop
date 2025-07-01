using System;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public struct DelayedVector3 {
        public Vector3 Target { get; private set; }
        public Vector3 Value { get; private set; }

        /// <summary> Move Value towards Target </summary>
        /// <returns> Was Value changed </returns>
        public bool Update(float deltaTime, float speed) {
            if (Value != Target) {
                Value = Vector3.MoveTowards(Value, Target, speed * deltaTime);
                return true;
            } else {
                return false;
            }
        }

        public void Set(Vector3 value) {
            Target = value;
        }

        public void SetInstant(Vector3 value) {
            Value = value;
            Target = value;
        }

        public bool IsStable => Value == Target;
    }
}