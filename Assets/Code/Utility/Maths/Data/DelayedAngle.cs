using Awaken.Utility;
using System;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public partial struct DelayedAngle {
        public ushort TypeForSerialization => SavedTypes.DelayedAngle;

        [ShowInInspector, ReadOnly, Saved] public float Target { get; private set; }
        [ShowInInspector, ReadOnly, Saved] public float Value { get; private set; }
        
        public bool IsStable => Value == Target;

        /// <summary> Move Value towards Target </summary>
        /// <returns> Was Value changed </returns>
        public bool Update(float deltaTime, float speed) {
            if (Value != Target) {
                Value = Mathf.MoveTowards(Value, Target, speed * deltaTime);
                return true;
            } else {
                return false;
            }
        }

        public void UpdateInstant() {
            Value = Target;
        }

        public void Set(float value) {
            Target = Mathf.Repeat(value, 360f);
            MakeValueDeltaSmallest();
        }

        public void SetInstant(float value) {
            Target = Mathf.Repeat(value, 360f);
            Value = Target;
        }

        public void SetValue(float value) {
            Value = value;
            MakeValueDeltaSmallest();
        }

        void MakeValueDeltaSmallest() {
            Value = Target - Mathf.DeltaAngle(Value, Target);
        }
    }
}