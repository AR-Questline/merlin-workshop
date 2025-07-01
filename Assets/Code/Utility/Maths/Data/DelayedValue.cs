using Awaken.Utility;
using System;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.Maths.Data {
    [Serializable]
    public partial struct DelayedValue {
        public ushort TypeForSerialization => SavedTypes.DelayedValue;

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
            Target = value;
        }

        public void SetInstant(float value) {
            Value = value;
            Target = value;
        }
    }
}