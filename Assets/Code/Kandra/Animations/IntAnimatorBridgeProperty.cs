using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Kandra.Animations {
    public sealed class IntAnimatorBridgeProperty : AnimatorBridgeProperty<int> {
        [HorizontalGroup, HideLabel] public int value;

        public override void Apply(Material[] materials) {
            materials[materialIndex].SetInt(_propertyId, value);
        }

        protected override bool HasProperty(Material material) {
            return material.HasInt(propertyName);
        }
    }
}
