using System;
using System.Collections.Generic;
using Awaken.TG.Editor.Utility;
using Sirenix.OdinInspector;
using Awaken.TG.Main.Heroes.Items;
using Sirenix.Utilities.Editor;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Awaken.TG.Editor.Graphics.IconRenderer {
    [Serializable]
    public class IconRendererCategory {
        [ToggleGroup("use", "$category", CollapseOthersOnExpand = false)]
        public bool use;
        [ToggleGroup("use", CollapseOthersOnExpand = false)]
        public string category;
        [ToggleGroup("use", CollapseOthersOnExpand = false)] [HideIf(nameof(rigTransform))]
        public bool fitToCamera;

        [HorizontalGroup("use/rig")] 
        public bool rigTransform;
        
        [ToggleGroup("use", CollapseOthersOnExpand = false)]
        [HideIf(nameof(FitToCameraOrRigTransform))]
        [OnValueChanged(nameof(OnTransformValuesChanged))]
        [InlineButton(nameof(GetFromScene))]
        public TransformValues transform = TransformValues.Default;

        [SerializeField] [HideInInspector] 
        public SerializableDictionary<string, TransformValues> rigTransforms;
        
        [ToggleGroup("use", CollapseOthersOnExpand = false)]
        [CustomValueDrawer(nameof(ItemTemplateIconRenderingSettingsDrawer))]
        [ListDrawerSettings(OnTitleBarGUI = nameof(DrawListTitleBar), AlwaysAddDefaultValue = true)]
        [SerializeField]
        List<IconRenderingSettings> iconsRenderingSettingsRenderingSettings = new();
        
        int _previewIndex;
        bool FitToCameraOrRigTransform => fitToCamera || rigTransform;
        public SerializableDictionary<string, TransformValues> RigTransforms => rigTransforms;
        public List<IconRenderingSettings> IconsRenderingSettings => iconsRenderingSettingsRenderingSettings;

        public IconRendererCategory(string name) {
            category = name;
        }

        [HorizontalGroup("use/rig")]
        [ShowIf(nameof(rigTransform))]
        [Button]
        void FromPreview() {
            if (IconRendererWindow.TryGetRig(out var rigValues)) {
                rigTransforms = rigValues;
            }
        }

        public IconRenderingSettings FindIconRenderingSettings(GameObject prefab) {
            if (IconsRenderingSettings == null) {
                return null;
            }

            foreach (var iconRenderingSettings in IconsRenderingSettings) {
                if (iconRenderingSettings == null || iconRenderingSettings.prefab == null) {
                    continue;
                }
                
                if (iconRenderingSettings.prefab == prefab) {
                    return iconRenderingSettings;
                }
            }
            return null;
        }

        public IconRenderingSettings ItemTemplateIconRenderingSettingsDrawer(IconRenderingSettings iconRenderingSettings) {
            // EditorGUILayout.BeginVertical();
            // EditorGUILayout.BeginHorizontal();
            // IconRenderingSettingsDrawUtilities.DrawButtons(this, iconRenderingSettings);
            // iconRenderingSettings.prefab = (GameObject)SirenixEditorFields.UnityObjectField(iconRenderingSettings.prefab, typeof(GameObject), false);
            // EditorGUILayout.EndHorizontal();
            //
            // IconRenderingSettingsDrawUtilities.DrawConditionalTransformSettings(this, iconRenderingSettings);
            //
            // EditorGUILayout.EndVertical();
            return iconRenderingSettings;
        }

        void OnTransformValuesChanged(TransformValues transformValues) => IconRenderer.Transform(transformValues);

        void DrawListTitleBar() {
            // if (SirenixEditorGUI.ToolbarButton(EditorIcons.Previous)) {
            //     _previewIndex--;
            //     if (_previewIndex < 0) {
            //         _previewIndex = IconsRenderingSettings.Count - 1;
            //     }
            //
            //     if (rigTransform) {
            //         IconRenderer.PreviewItem(IconsRenderingSettings[_previewIndex], null, rigTransforms);
            //     } else {
            //         IconRenderer.PreviewItem(IconsRenderingSettings[_previewIndex], transform);
            //     }
            // }
            //
            // SirenixEditorGUI.ToolbarButton(EditorIcons.MagnifyingGlass);
            // if (SirenixEditorGUI.ToolbarButton(EditorIcons.Next)) {
            //     _previewIndex++;
            //     if (_previewIndex >= IconsRenderingSettings.Count) {
            //         _previewIndex = 0;
            //     }
            //
            //     if (rigTransform) {
            //         IconRenderer.PreviewItem(IconsRenderingSettings[_previewIndex], null, rigTransforms);
            //     } else {
            //         IconRenderer.PreviewItem(IconsRenderingSettings[_previewIndex], transform);
            //     }
            // }
        }

        void GetFromScene() {
            var values = IconRenderer.GetRenderObjectParentTransformValues();
            if (values != null) {
                transform = values.Value;
            }
        }

        public void Add(GameObject prefab) {
            IconRenderingSettings iconRenderingSettings = new() {
                prefab = prefab
            };
            Add(iconRenderingSettings);
        }

        public void Add(IconRenderingSettings iconRenderingSettings) {
            Undo.RecordObject(IconRenderer.Settings, "Add item template icon rendering settings");
            PrefabUtility.RecordPrefabInstancePropertyModifications(IconRenderer.Settings);
            IconsRenderingSettings.Add(iconRenderingSettings);
        }

        public void Remove(GameObject prefab) {
            IconRenderingSettings iconRenderingSettings = FindIconRenderingSettings(prefab);
            if (iconRenderingSettings != null) {
                Remove(iconRenderingSettings);
            }
        }

        public void Remove(IconRenderingSettings iconRenderingSettings) {
            Undo.RecordObject(IconRenderer.Settings, "Remove item template icon rendering settings");
            IconsRenderingSettings.Remove(iconRenderingSettings);
        }
    }
}