using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Kandra.Animations {
    public sealed class VectorAnimatorBridgeProperty : AnimatorBridgeProperty<Vector4> {
        [HorizontalGroup, HideLabel] public Vector4 value;

        public override void Apply(Material[] materials) {
            materials[materialIndex].SetVector(_propertyId, value);
        }

        protected override bool HasProperty(Material material) {
            return material.HasVector(propertyName);
        }
    }
}
