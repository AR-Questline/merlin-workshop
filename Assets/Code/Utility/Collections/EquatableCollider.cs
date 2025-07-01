using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Utility.Collections {
    [Serializable, InlineProperty]
    public struct EquatableCollider : IEquatable<EquatableCollider> {
        [HideLabel] public Collider collider;

        public EquatableCollider(Collider collider) {
            this.collider = collider;
        }
            
        public bool Equals(EquatableCollider other) {
            return collider == other.collider;
        }
        
        public static implicit operator Collider(EquatableCollider equatableCollider) => equatableCollider.collider;
        public static implicit operator EquatableCollider(Collider collider) => new(collider);
    }
}