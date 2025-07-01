using System;
using System.Collections.Generic;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility.Assets;
using Awaken.Utility.Editor;
using Awaken.Utility.Editor.SearchableMenu;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Awaken.ECS.Editor.DrakeRenderer {
    [CustomEditor(typeof(DrakeBatchMaterialOverride))]
    public class DrakeBatchMaterialOverrideEditor : OdinEditor {
        static DrakeBatchMaterialOverride s_instance;
        
        public override void OnInspectorGUI() {
            s_instance = (DrakeBatchMaterialOverride)target;
            base.OnInspectorGUI();
            s_instance = null;
        }

        [CustomPropertyDrawer(typeof(DrakeBatchMaterialOverride.Replacement))]
        class ReplacementEditor : PropertyDrawer {
            const float ArrowWidth = 28;
            const float ChooseFromSceneWidth = 18;
            
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                if (s_instance == null) {
                    EditorGUI.LabelField(position, "DrakeBatchMaterialOverride instance not found");
                    return;
                }

                float halfWidth = position.width * 0.5f;
                var originalRect = position;
                originalRect.width = halfWidth - ArrowWidth / 2 - ChooseFromSceneWidth;
                var chooseFromSceneRect = position;
                chooseFromSceneRect.x = originalRect.xMax;
                chooseFromSceneRect.width = ChooseFromSceneWidth;
                var arrowRect = position;
                arrowRect.x = chooseFromSceneRect.xMax;
                arrowRect.width = ArrowWidth;
                var replacementRect = position;
                replacementRect.x = arrowRect.xMax;
                replacementRect.width = halfWidth - ArrowWidth / 2;

                var originalProperty = property.FindPropertyRelative(nameof(DrakeBatchMaterialOverride.Replacement.original));
                var replacementProperty = property.FindPropertyRelative(nameof(DrakeBatchMaterialOverride.Replacement.replacement));
                
                EditorGUI.PropertyField(originalRect, originalProperty, GUIContent.none);
                if (GUI.Button(chooseFromSceneRect, GUIUtils.Content("S", "Choose from scene"))) {
                    ChooseFromScene(new Vector2(originalRect.xMin, originalRect.yMax), originalProperty);
                }
                EditorGUI.LabelField(arrowRect, "→", EditorStyles.centeredGreyMiniLabel);
                EditorGUI.PropertyField(replacementRect, replacementProperty, GUIContent.none);
            }

            void ChooseFromScene(Vector2 position, SerializedProperty originalProperty) {
                var references = new HashSet<AssetReference>(AssetReferenceUtils.EqualityComparer.Instance);
                var access = new DrakeBatchMaterialOverride.EditorAccess(s_instance);
                foreach (var root in access.Roots) {
                    foreach (var renderer in root.GetComponentsInChildren<DrakeMeshRenderer>()) {
                        foreach (var reference in renderer.MaterialReferences) {
                            references.Add(reference);
                        }
                    }
                }

                var materials = new MaterialToReplace[references.Count];
                int index = 0;
                foreach (var reference in references) {
                    var material = reference.EditorLoad<Material>();
                    materials[index++] = new MaterialToReplace {
                        reference = reference,
                        name = material != null ? material.name : "(null)"
                    };
                }
                Array.Sort(materials, (lhs, rhs) => string.Compare(lhs.name, rhs.name, StringComparison.Ordinal));
                
                var menu = ScriptableObject.CreateInstance<SearchableMenuPresenter>();
                foreach (var material in materials) {
                    menu.AddEntry(material.name, () => {
                        originalProperty.boxedValue = material.reference;
                        originalProperty.serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAt(position);
            }

            struct MaterialToReplace {
                public AssetReference reference;
                public string name;
            }
        }
    }
}