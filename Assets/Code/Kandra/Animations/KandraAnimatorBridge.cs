using System;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.Kandra.Animations {
    public class KandraAnimatorBridge : MonoBehaviour {
        public KandraRenderer kandraRenderer;
        [NonSerialized] Material[] _materialsToAnimate;

        public AnimatorBridgeProperty[] properties;

        int _validPropertiesCount;

        void Awake() {
            kandraRenderer.EnsureInitialized();
            _materialsToAnimate = kandraRenderer.UseInstancedMaterials();

            _validPropertiesCount = properties.Length;
            for (int i = properties.Length - 1; i >= 0; i--) {
                if (!properties[i].IsValid(_materialsToAnimate)) {
                    _validPropertiesCount--;
                    properties[i] = properties[_validPropertiesCount];
                    Destroy(properties[_validPropertiesCount].gameObject);
                }
            }
        }

        void Update() {
            for (int i = 0; i < _validPropertiesCount; i++) {
                AnimatorBridgeProperty property = properties[i];
                property.Apply(_materialsToAnimate);
            }
        }

        void OnDestroy() {
            _materialsToAnimate = null;
            kandraRenderer.UseOriginalMaterials();
        }

        // === Editor
#if UNITY_EDITOR
        void OnValidate() {
            if (!kandraRenderer) {
                kandraRenderer = GetComponent<KandraRenderer>();
                if (!kandraRenderer) {
                    return;
                }
            }
            var originalMaterials = kandraRenderer.rendererData.materials;
            for (int i = properties.Length - 1; i >= 0; i--) {
                if (!properties[i].IsValid(originalMaterials)) {
                    EditorRemoveAnimatorProperty(i);
                }
            }
        }

        public void EditorRemoveAnimatorProperty(int index) {
            var property = properties[index];
            ArrayUtils.RemoveAt(ref properties, index);
            DestroyImmediate(property.gameObject, true);
        }

        public void EditorValueChanged() {
            if (_materialsToAnimate == null || kandraRenderer == null || properties == null) {
                return;
            }

            for (int i = 0; i < properties.Length; i++) {
                AnimatorBridgeProperty property = properties[i];
                property.CachePropertyId();
                if (!property.IsValid(_materialsToAnimate)) {
                    continue;
                }
                property.Apply(_materialsToAnimate);
            }
        }

        public void EDITOR_EnterPreviewMode() {
            Awake();
        }

        public void EDITOR_ExitPreviewMode() {
            OnDestroy();
        }

        public bool IsInPreviewMode() {
            return _materialsToAnimate != null;
        }

        public bool HasValidRenderer() {
            return kandraRenderer != null;
        }
#endif
    }
}
