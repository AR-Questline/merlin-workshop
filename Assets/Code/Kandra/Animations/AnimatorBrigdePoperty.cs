using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Kandra.Animations {
    [InlineProperty, HideLabel]
    public abstract class AnimatorBridgeProperty : MonoBehaviour {
        [HideInInlineEditors] public int materialIndex;
        [ReadOnly, HideLabel, HorizontalGroup] public string propertyName;
        protected int _propertyId;

        void Awake() {
            CachePropertyId();
        }

        public void CachePropertyId() {
            _propertyId = Shader.PropertyToID(propertyName);
        }

        public abstract void Apply(Material[] materials);

        public bool IsValid(Material[] materials) {
            if (materialIndex >= materials.Length) {
                return false;
            }
            var material = materials[materialIndex];
            if (material == null) {
                return false;
            }

            return HasProperty(material);
        }

        protected abstract bool HasProperty(Material material);
    }

    public abstract class AnimatorBridgeProperty<T> : AnimatorBridgeProperty {}
}
