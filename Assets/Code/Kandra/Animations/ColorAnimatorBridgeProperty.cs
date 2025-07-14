using UnityEngine;

namespace Awaken.Kandra.Animations {
    public sealed class ColorAnimatorBridgeProperty : AnimatorBridgeProperty<Color> {
        public Color value;

        public override void Apply(Material[] materials) {
            materials[materialIndex].SetColor(_propertyId, value);
        }

        protected override bool HasProperty(Material material) {
            return material.HasColor(propertyName);
        }
    }
}
