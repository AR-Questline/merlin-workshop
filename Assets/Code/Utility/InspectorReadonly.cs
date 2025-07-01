using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility {
    /// <summary> helper struct to make a field not assignable in inspector, but not disabled as readonly would do </summary>
    [Serializable, InlineProperty]
    public struct InspectorReadonly<T> {
        [SerializeField, HideInInspector] T value;

        public InspectorReadonly(T value) {
            this.value = value;
        }

        [ShowInInspector, HideLabel]
        public T Value {
            get => value;
            set { }
        }
        
        public static implicit operator InspectorReadonly<T>(T value) => new(value);
        public static implicit operator T(InspectorReadonly<T> value) => value.Value;
    }
}