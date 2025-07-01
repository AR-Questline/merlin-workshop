using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    [EditorToolbarElement("DrakeHighestLodToolbar", typeof(SceneView))]
    public class DrakeHighestLodToolbarButton : EditorToolbarButton {
        public static event Action HighestLodModeChanged;
        public static bool HighestLodMode {
            get => EditorPrefs.GetBool("DrakeRenderer.HighestLodMode", false);
            set {
                if (HighestLodMode != value) {
                    EditorPrefs.SetBool("DrakeRenderer.HighestLodMode", value);
                    HighestLodModeChanged?.Invoke();
                }
            }
        }

        public DrakeHighestLodToolbarButton() {
            text = "Drake";
            clicked += OnClick;
            UpdateVisuals();
        }

        void OnClick() {
            HighestLodMode = !HighestLodMode;
            UpdateVisuals();
        }
        void UpdateVisuals() {
            var iconName = HighestLodMode ?
                "MeshRenderer Icon" :
                "LODGroup Icon";
            icon = (Texture2D)EditorGUIUtility.IconContent(iconName).image;
            tooltip = HighestLodMode ? "Drake shows always highest lod" : "Drake lods working normally";
        }
    }
}
