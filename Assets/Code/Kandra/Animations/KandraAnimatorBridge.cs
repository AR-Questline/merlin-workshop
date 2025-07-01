using System;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.Kandra.Animations {
    public class KandraAnimatorBridge : MonoBehaviour {
        public KandraRenderer kandraRenderer;
        [NonSerialized] Material[] _materialsToAnimate;

        [HideReferenceObjectPicker, OnValueChanged(nameof(EditorValueChanged), true)]
        [InlineEditor(Expanded = true, ObjectFieldMode = InlineEditorObjectFieldModes.Hidden), ListDrawerSettings(ShowFoldout = false, ShowPaging = false, DefaultExpandedState = true, HideAddButton = true, CustomRemoveIndexFunction = nameof(EditorRemoveAnimatorProperty))]
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

        void EditorRemoveAnimatorProperty(int index) {
            var property = properties[index];
            ArrayUtils.RemoveAt(ref properties, index);
            DestroyImmediate(property.gameObject, true);
        }

        void EditorValueChanged() {
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

#if UNITY_EDITOR
        [Button, HideIf(nameof(IsInPreviewMode)), EnableIf(nameof(HasValidRenderer))]
        void EDITOR_EnterPreviewMode() {
            Awake();
        }

        [Button, ShowIf(nameof(IsInPreviewMode)), EnableIf(nameof(HasValidRenderer))]
        void EDITOR_ExitPreviewMode() {
            OnDestroy();
        }

        bool IsInPreviewMode() {
            return _materialsToAnimate != null;
        }

        bool HasValidRenderer() {
            return kandraRenderer != null;
        }
#endif
    }
}
