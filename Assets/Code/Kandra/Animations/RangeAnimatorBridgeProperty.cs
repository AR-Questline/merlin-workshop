using UnityEngine;

namespace Awaken.Kandra.Animations {
    public class RangeAnimatorBridgeProperty : AnimatorBridgeProperty<float> {
        public float value;

        public float minValue;
        public float maxValue;

        public override void Apply(Material[] materials) {
            materials[materialIndex].SetFloat(_propertyId, value);
        }

        protected override bool HasProperty(Material material) {
            return material.HasFloat(propertyName);
        }
    }
}
