using System;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public struct DelayedVector2 {
        public Vector2 Target { get; private set; }
        public Vector2 Value { get; private set; }

        /// <summary> Move Value towards Target </summary>
        /// <returns> Was Value changed </returns>
        public bool Update(float deltaTime, float speed) {
            if (Value != Target) {
                Value = Vector2.MoveTowards(Value, Target, speed * deltaTime);
                return true;
            } else {
                return false;
            }
        }

        public void Set(Vector2 value) {
            Target = value;
        }

        public void SetInstant(Vector2 value) {
            Value = value;
            Target = value;
        }

        public bool IsStable => Value == Target;
    }
}