using System;
using System.Collections.Generic;
using Awaken.Kandra.Animations;
using Awaken.Utility.Maths;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    [CustomEditor(typeof(KandraAnimatorBridge))]
    public class KandraAnimatorBridgeEditor : UnityEditor.Editor {
        static readonly HashSet<string> PropertiesBlackList = new HashSet<string> {
            "_Surface",
        };

        string[] _cachedPaths = Array.Empty<string>();
        MaterialProperty[] _cachedProperties;
        int _cachedHash;

        protected void OnEnable() {
            var props = ((KandraAnimatorBridge)target).properties ?? Array.Empty<AnimatorBridgeProperty>();
            foreach (var bridgeProperty in props) {
                bridgeProperty.gameObject.hideFlags = HideFlags.NotEditable | HideFlags.HideInHierarchy;
            }
        }

        public override void OnInspectorGUI() {
            var bridge = (KandraAnimatorBridge)target;

            EditorGUI.BeginChangeCheck();
            bridge.kandraRenderer = (KandraRenderer)EditorGUILayout.ObjectField("Kandra Renderer", bridge.kandraRenderer, typeof(KandraRenderer), true);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
            var properties = bridge.properties;
            for (var i = 0; i < properties.Length; i++) {
                var property = properties[i];
                EditorGUILayout.BeginHorizontal();

                if (property is RangeAnimatorBridgeProperty rangeProperty) {
                    rangeProperty.value = EditorGUILayout.Slider(property.propertyName, rangeProperty.value, rangeProperty.minValue, rangeProperty.maxValue);
                } else if (property is FloatAnimatorBridgeProperty floatProperty) {
                    floatProperty.value = EditorGUILayout.FloatField(property.propertyName, floatProperty.value);
                } else if (property is ColorAnimatorBridgeProperty colorProperty) {
                    colorProperty.value = EditorGUILayout.ColorField(property.propertyName, colorProperty.value);
                } else if (property is VectorAnimatorBridgeProperty vectorProperty) {
                    vectorProperty.value = EditorGUILayout.Vector4Field(property.propertyName, vectorProperty.value);
                } else if (property is IntAnimatorBridgeProperty intProperty) {
                    intProperty.value = EditorGUILayout.IntField(property.propertyName, intProperty.value);
                }

                if (GUILayout.Button("X", GUILayout.Width(32))) {
                    Undo.RecordObject(target, "Remove Property Override");
                    bridge.EditorRemoveAnimatorProperty(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            int selectedIndex = EditorGUILayout.Popup(new GUIContent("Add Override"), -1, _cachedPaths);
            if (selectedIndex >= 0) {
                var property = _cachedProperties[selectedIndex];
                AddPropertyOverride(bridge, property);
            }

            EditorGUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck()) {
                bridge.EditorValueChanged();
                EditorUtility.SetDirty(bridge);
            }

            if (!bridge.kandraRenderer) {
                return;
            }

            using (new EditorGUI.DisabledScope(!bridge.HasValidRenderer())) {
                if (bridge.IsInPreviewMode()) {
                    if (GUILayout.Button("Exit Preview Mode")) {
                        bridge.EDITOR_ExitPreviewMode();
                    }
                } else {
                    if (GUILayout.Button("Enter Preview Mode")) {
                        bridge.EDITOR_EnterPreviewMode();
                    }
                }
            }

            UpdateProperties(bridge);
        }

        void UpdateProperties(KandraAnimatorBridge kandraAnimatorBridge) {
            var materials = kandraAnimatorBridge.kandraRenderer.rendererData.materials;
            if (materials.Length == 0) {
                _cachedPaths = Array.Empty<string>();
                _cachedProperties = Array.Empty<MaterialProperty>();
                _cachedHash = 0;
                return;
            }

            var materialsHash = materials[0].GetHashCode();
            for (int i = 1; i < materials.Length; i++) {
                materialsHash = DHash.Combine(materials[i].GetHashCode(), materialsHash);
            }

            if (_cachedHash == materialsHash) {
                return;
            }

            var paths = new List<string>();
            var properties = new List<MaterialProperty>();

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++) {
                Material material = materials[materialIndex];
                var shader = material.shader;
                for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++) {
                    var propertyName = ShaderUtil.GetPropertyName(shader, i);
                    if (ShaderUtil.IsShaderPropertyHidden(shader, i)) {
                        continue;
                    }
                    if (PropertiesBlackList.Contains(propertyName)) {
                        continue;
                    }

                    var propertyType = ShaderUtil.GetPropertyType(shader, i);
                    if (propertyType == ShaderUtil.ShaderPropertyType.TexEnv) {
                        continue;
                    }

                    if (HasOverride(materialIndex, propertyName)) {
                        continue;
                    }

                    var materialProperty = new MaterialProperty {
                        shader = shader,
                        material = material,
                        materialIndex = materialIndex,
                        propertyName = propertyName,
                        propertyIndex = i,
                        propertyType = propertyType
                    };
                    paths.Add($"[{materialIndex}]_{material.name}/{propertyName}: {propertyType}");
                    properties.Add(materialProperty);
                }
            }

            _cachedPaths = paths.ToArray();
            _cachedProperties = properties.ToArray();
            _cachedHash = materialsHash;

            bool HasOverride(int materialIndex, string propertyName) {
                foreach (var property in kandraAnimatorBridge.properties) {
                    if (property.materialIndex == materialIndex && property.propertyName == propertyName) {
                        return true;
                    }
                }

                return false;
            }
        }

        void AddPropertyOverride(KandraAnimatorBridge kandraAnimatorBridge, in MaterialProperty property) {
            serializedObject.ApplyModifiedProperties();

            Undo.SetCurrentGroupName("Add Property Override");

            Undo.RegisterCompleteObjectUndo(kandraAnimatorBridge, "Add Property Override");
            var properties = kandraAnimatorBridge.properties;
            Array.Resize(ref properties, properties.Length + 1);

            var overrideGO = new GameObject($"[{property.materialIndex}].{property.propertyName}");
            Undo.RegisterCreatedObjectUndo(overrideGO, "Created Property Override go");
            overrideGO.transform.SetParent(kandraAnimatorBridge.transform);
            overrideGO.hideFlags = HideFlags.NotEditable | HideFlags.HideInHierarchy;

            var propertyOverride = property.AddBridgeProperty(overrideGO);
            properties[properties.Length - 1] = propertyOverride;

            kandraAnimatorBridge.properties = properties;
            EditorUtility.SetDirty(kandraAnimatorBridge);

            _cachedHash = 0;

            serializedObject.Update();
        }

        struct MaterialProperty {
            public Shader shader;
            public Material material;
            public int materialIndex;
            public string propertyName;
            public int propertyIndex;
            public ShaderUtil.ShaderPropertyType propertyType;

            public readonly AnimatorBridgeProperty AddBridgeProperty(GameObject gameObject) {
                var propertyOverride = (AnimatorBridgeProperty)gameObject.AddComponent(PropertyOverrideType);
                propertyOverride.materialIndex = materialIndex;
                propertyOverride.propertyName = propertyName;

                if (propertyType == ShaderUtil.ShaderPropertyType.Range) {
                    var rangeProperty = (RangeAnimatorBridgeProperty)propertyOverride;
                    rangeProperty.minValue = ShaderUtil.GetRangeLimits(shader, propertyIndex, 1);
                    rangeProperty.maxValue = ShaderUtil.GetRangeLimits(shader, propertyIndex, 2);
                    rangeProperty.value = material.GetFloat(propertyName);
                } else if (propertyType == ShaderUtil.ShaderPropertyType.Float) {
                    var floatProperty = (FloatAnimatorBridgeProperty)propertyOverride;
                    floatProperty.value = material.GetFloat(propertyName);
                } else if (propertyType == ShaderUtil.ShaderPropertyType.Color) {
                    var colorProperty = (ColorAnimatorBridgeProperty)propertyOverride;
                    colorProperty.value = material.GetColor(propertyName);
                } else if (propertyType == ShaderUtil.ShaderPropertyType.Vector) {
                    var vectorProperty = (VectorAnimatorBridgeProperty)propertyOverride;
                    vectorProperty.value = material.GetVector(propertyName);
                } else if (propertyType == ShaderUtil.ShaderPropertyType.Int) {
                    var intProperty = (IntAnimatorBridgeProperty)propertyOverride;
                    intProperty.value = material.GetInt(propertyName);
                }

                return propertyOverride;
            }

            readonly Type PropertyOverrideType => propertyType switch {
                ShaderUtil.ShaderPropertyType.Color  => typeof(ColorAnimatorBridgeProperty),
                ShaderUtil.ShaderPropertyType.Vector => typeof(VectorAnimatorBridgeProperty),
                ShaderUtil.ShaderPropertyType.Float  => typeof(FloatAnimatorBridgeProperty),
                ShaderUtil.ShaderPropertyType.Range  => typeof(RangeAnimatorBridgeProperty),
                ShaderUtil.ShaderPropertyType.Int    => typeof(IntAnimatorBridgeProperty),
                _                                    => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
