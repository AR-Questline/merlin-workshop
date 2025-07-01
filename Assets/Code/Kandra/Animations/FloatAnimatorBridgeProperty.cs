using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Kandra.Animations {
    public sealed class FloatAnimatorBridgeProperty : AnimatorBridgeProperty<float> {
        [HorizontalGroup, HideLabel] public float value;

        public override void Apply(Material[] materials) {
            materials[materialIndex].SetFloat(_propertyId, value);
        }

        protected override bool HasProperty(Material material) {
            return material.HasFloat(propertyName);
        }
    }
}
