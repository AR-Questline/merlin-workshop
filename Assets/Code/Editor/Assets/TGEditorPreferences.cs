using System;
using System.Collections.Generic;
using System.Reflection;
using Awaken.TG.Editor.DataViews.Data;
using Awaken.TG.Editor.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets {
    // ReSharper disable once InconsistentNaming
    public class TGEditorPreferences : ScriptableObject {
        public static TGEditorPreferences Instance => s_instance = s_instance ? s_instance : GetOrCreateSettings();
        static TGEditorPreferences s_instance;

        const string SettingsPath = "Assets/Code/Editor/TGEditorPreferences.asset";

        [OnValueChanged(nameof(UpdateEditorPreferences))]
        public bool hideFMODProjectFiles = true;
        public bool showTerms = false;
        public bool showTermsInGraphs = false;
        [OnValueChanged(nameof(UpdateColor))]
        public Color listItemColorEven = new(0.33f, 0.33f, 0.33f, 1f);
        [OnValueChanged(nameof(UpdateColor))]
        public Color listItemColorOdd = new(0.22f, 0.22f, 0.22f, 1f);
        public bool customRigInClothesPreview;
        [Space]
        public bool nicifyModelsDebugButtons;
        public string classMethodSeparator = ".";
        public int modelsDebugUpdateInterval = 2;
        public bool disableMipmapsStreaming;
        public DataViewPreferences dataView;

        // ReSharper disable NotAccessedField.Global
        [Space, Header("Previews")]
        [OnValueChanged(nameof(UpdateEditorPreferences))] public bool disableAllPreviews = false;
        [OnValueChanged(nameof(UpdateEditorPreferences))] public bool disableRegrowablePreviews = false;
        [OnValueChanged(nameof(UpdateEditorPreferences))] public bool disablePickablePreviews = false;
        [OnValueChanged(nameof(UpdateEditorPreferences))] public bool disableLocationPreviews = false;
        [Space, Header("ECS")]
        [OnValueChanged(nameof(UpdateEditorPreferences))] public bool showEntities = false;
        [OnValueChanged(nameof(UpdateEditorPreferences))] public bool showDrakeBounds = false;
        [Space, Header("Other")]
        [OnValueChanged(nameof(UpdateEditorPreferences))] public bool debugTranslations = false;
        [OnValueChanged(nameof(UpdateEditorPreferences))] public bool disabledHideFlagsHeader = false;
        [OnValueChanged(nameof(UpdateEditorPreferences))] public bool showKandraSkinnedVertices = false;

        [Space, Header("AR preview")]
        [OnValueChanged(nameof(UpdateEditorPreferences)), Range(0.1f, 10f)]
        public float lightIntensityMainDrakePreview = 1;
        [OnValueChanged(nameof(UpdateEditorPreferences))]
        public Vector3 lightDirectionMainDrakePreview = new Vector3(50f, 50f, 0);
        [OnValueChanged(nameof(UpdateEditorPreferences))]
        public Color lightColorMainDrakePreview = new Color(0.769f, 0.769f, 0.769f, 1f);

        [OnValueChanged(nameof(UpdateEditorPreferences)), Range(0.1f, 10f)]
        public float lightIntensitySecondaryDrakePreview = 1;
        [OnValueChanged(nameof(UpdateEditorPreferences))]
        public Vector3 lightDirectionSecondaryDrakePreview = new Vector3(340f, 218f, 177f);
        [OnValueChanged(nameof(UpdateEditorPreferences))]
        public Color lightColorSecondaryDrakePreview = new Color(0.28f, 0.28f, 0.315f, 0.0f);

        [OnValueChanged(nameof(UpdateEditorPreferences)), LabelText("Ambient Color")]
        public Color ambientColorDrakePreview = new Color(.2f, .2f, .2f, 0);
        [OnValueChanged(nameof(UpdateEditorPreferences)), LabelText("Background Color")]
        public Color backgroundColorDrakePreview = new Color(0.85f, 0.85f, 0.85f, 1f);
        // ReSharper restore NotAccessedField.Global

        static TGEditorPreferences GetOrCreateSettings() {
            TGEditorPreferences settings = AssetDatabase.LoadAssetAtPath<TGEditorPreferences>(SettingsPath);
            if (settings == null) {
                settings = CreateInstance<TGEditorPreferences>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }

            settings.LoadFromEditorPrefs();
            return settings;
        }

        static SerializedObject GetSerializedSettings() {
            return new(GetOrCreateSettings());
        }

        public void UpdateColor() {
            OdinListItemColors.SetColors();
        }

        void UpdateEditorPreferences(string propertyName) {
            var field = this.GetType().GetField(propertyName);
            if (field.FieldType == typeof(bool)) {
                EditorPrefs.SetBool(propertyName, (bool)field.GetValue(this));
            } else if (field.FieldType == typeof(string)) {
                EditorPrefs.SetString(propertyName, (string)field.GetValue(this));
            } else if (field.FieldType == typeof(int)) {
                EditorPrefs.SetInt(propertyName, (int)field.GetValue(this));
            } else if (field.FieldType == typeof(float)) {
                EditorPrefs.SetFloat(propertyName, (float)field.GetValue(this));
            } else if (field.FieldType == typeof(Vector3)) {
                var vectorValue = (Vector3)field.GetValue(this);
                EditorPrefs.SetFloat(propertyName+".x", vectorValue.x);
                EditorPrefs.SetFloat(propertyName+".y", vectorValue.y);
                EditorPrefs.SetFloat(propertyName+".z", vectorValue.z);
            } else if (field.FieldType == typeof(Color)) {
                var colorValue = (Color)field.GetValue(this);
                EditorPrefs.SetFloat(propertyName+".r", colorValue.r);
                EditorPrefs.SetFloat(propertyName+".g", colorValue.g);
                EditorPrefs.SetFloat(propertyName+".b", colorValue.b);
                EditorPrefs.SetFloat(propertyName+".a", colorValue.a);
            }
        }

        void LoadFromEditorPrefs() {
            foreach (var field in this.GetType().GetFields()) {
                var onValueChanged = field.GetAttribute<OnValueChangedAttribute>();
                if (onValueChanged?.Action == nameof(UpdateEditorPreferences)) {
                    LoadFromEditorPrefs(field.Name);
                }
            }
        }

        void LoadFromEditorPrefs(string propertyName) {
            var field = this.GetType().GetField(propertyName);
            if (field.FieldType == typeof(bool)) {
                field.SetValue(this, EditorPrefs.GetBool(propertyName, (bool)field.GetValue(this)));
            } else if (field.FieldType == typeof(string)) {
                field.SetValue(this, EditorPrefs.GetString(propertyName, (string)field.GetValue(this)));
            } else if (field.FieldType == typeof(int)) {
                field.SetValue(this, EditorPrefs.GetInt(propertyName, (int)field.GetValue(this)));
            }
            if (field.FieldType == typeof(float)) {
                field.SetValue(this, EditorPrefs.GetFloat(propertyName, (float)field.GetValue(this)));
            }
            if (field.FieldType == typeof(Vector3)) {
                var vectorValue = (Vector3)field.GetValue(this);
                vectorValue.x = EditorPrefs.GetFloat(propertyName+".x", vectorValue.x);
                vectorValue.y = EditorPrefs.GetFloat(propertyName+".y", vectorValue.y);
                vectorValue.z = EditorPrefs.GetFloat(propertyName+".z", vectorValue.z);
                field.SetValue(this, vectorValue);
            }
            if (field.FieldType == typeof(Color)) {
                var colorValue = (Color)field.GetValue(this);
                colorValue.r = EditorPrefs.GetFloat(propertyName+".r", colorValue.r);
                colorValue.g = EditorPrefs.GetFloat(propertyName+".g", colorValue.g);
                colorValue.b = EditorPrefs.GetFloat(propertyName+".b", colorValue.b);
                colorValue.a = EditorPrefs.GetFloat(propertyName+".a", colorValue.a);
                field.SetValue(this, colorValue);
            }
        }

        // ReSharper disable once InconsistentNaming
        public class TGEditorPreferencesProvider : SettingsProvider {
            SerializedObject _customSettings;

            SerializedObject CustomSettings => _customSettings ??= new(TGEditorPreferences.Instance);

            TGEditorPreferencesProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) {}

            public override void OnGUI(string searchContext) {
                var searchParts = !string.IsNullOrWhiteSpace(searchContext) ?
                    searchContext.Split(' ') :
                    Array.Empty<string>();
                // Iterate over all properties and draw all non excluded
                CustomSettings.Update();
                SerializedProperty iterator = CustomSettings.GetIterator();
                for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false) {
                    if (iterator.name.Contains("m_Script") || !iterator.displayName.ContainsAny(searchParts)) {
                        continue;
                    }
                    using var changeGroup = new TGGUILayout.CheckChangeScope();

                    GUIContent labelText = null;
                    var labelTextAttribute = iterator.ExtractAttribute<LabelTextAttribute>();
                    if (labelTextAttribute != null) {
                        if (labelTextAttribute.NicifyText) {
                            labelText = new GUIContent(ObjectNames.NicifyVariableName(labelTextAttribute.Text), iterator.tooltip);
                        } else {
                            labelText = new GUIContent(labelTextAttribute.Text, iterator.tooltip);
                        }
                    }

                    EditorGUILayout.PropertyField(iterator, labelText);

                    if (!changeGroup) {
                        continue;
                    }
                    var onValueChanged = iterator.ExtractAttribute<OnValueChangedAttribute>();
                    if (onValueChanged == null) {
                        continue;
                    }
                    // If there is onValueChanged then we want to call it after value it updated
                    CustomSettings.ApplyModifiedPropertiesWithoutUndo();
                    object parent = iterator.GetParentValue();
                    var method = parent.GetType().GetMethod(onValueChanged.Action,
                        BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.NonPublic);
                    var parameters = method.GetParameters();
                    if (parameters.Length == 0) {
                        method.Invoke(parent, null);
                    }else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string)) {
                        method.Invoke(parent, new object[] { iterator.name });
                    }
                }
                CustomSettings.ApplyModifiedProperties();
            }

            public override bool HasSearchInterest(string searchContext) {
                var searchParts = !string.IsNullOrWhiteSpace(searchContext) ?
                    searchContext.Split(' ') :
                    Array.Empty<string>();
                CustomSettings.Update();
                SerializedProperty iterator = CustomSettings.GetIterator();
                for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false) {
                    if (iterator.displayName.ContainsAny(searchParts)) {
                        return true;
                    }
                }
                return base.HasSearchInterest(searchContext);
            }

            // Register the SettingsProvider
            [SettingsProvider]
            public static SettingsProvider CreateMyCustomSettingsProvider() {
                TGEditorPreferencesProvider provider = new TGEditorPreferencesProvider("Preferences/TG") {
                    keywords = new HashSet<string>(new[] { "ar", "tg" }),
                };
                return provider;
            }
        }
    }
}