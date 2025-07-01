using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Kandra.Animations {
    public class RangeAnimatorBridgeProperty : AnimatorBridgeProperty<float> {
        [HorizontalGroup, HideLabel, PropertyRange(minGetter: nameof(minValue), maxGetter: nameof(maxValue))] public float value;

        [HideInInlineEditors] public float minValue;
        [HideInInlineEditors] public float maxValue;

        public override void Apply(Material[] materials) {
            materials[materialIndex].SetFloat(_propertyId, value);
        }

        protected override bool HasProperty(Material material) {
            return material.HasFloat(propertyName);
        }
    }
}
