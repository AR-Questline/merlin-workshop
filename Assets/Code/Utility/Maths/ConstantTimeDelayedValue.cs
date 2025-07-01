using UnityEngine;

namespace Awaken.Utility.Maths {
    /// <summary>
    /// DelayedValue that after each change moves towards Target with the same amount of time
    /// </summary>
    public struct ConstantTimeDelayedValue {
        readonly float _delay;
        float _speed;
        
        public float Target { get; private set; }
        public float Value { get; private set; }

        public ConstantTimeDelayedValue(float delay, float value = 0) {
            _delay = delay;
            _speed = 1;
            Target = value;
            Value = value;
        }
        
        /// <summary> Move Value towards Target </summary>
        /// <returns> Was Value changed </returns>
        public bool Update(float deltaTime) {
            if (Value != Target) {
                Value = Mathf.MoveTowards(Value, Target, _speed * deltaTime);
                return true;
            } else {
                return false;
            }
        }

        public void Set(float value) {
            Target = value;
            _speed = Mathf.Abs(Target - Value) / _delay;
        }
        
        public void SetInstant(float value) {
            Value = value;
            Target = value;
        }
    }
}