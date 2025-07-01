using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Awaken.ECS.Editor.DrakeRenderer {
    [EditorToolbarElement("DrakeHackToolbar", typeof(SceneView))]
    public class DrakeHackToolbarButton : EditorToolbarButton {
        public static event Action SceneAuthoringHackChanged;
        public static bool SceneAuthoringHack {
            get => EditorPrefs.GetBool("DrakeRenderer.SceneAuthoringHack", false);
            set {
                if (SceneAuthoringHack != value) {
                    EditorPrefs.SetBool("DrakeRenderer.SceneAuthoringHack", value);
                    SceneAuthoringHackChanged?.Invoke();
                }
            }
        }

        public DrakeHackToolbarButton() {
            text = "Drake";
            clicked += OnClick;
            UpdateVisuals();
        }

        void OnClick() {
            SceneAuthoringHack = !SceneAuthoringHack;
            UpdateVisuals();
        }
        void UpdateVisuals() {
            var iconName = SceneAuthoringHack ?
                "d_scenepicking_pickable-mixed_hover" :
                "d_scenepicking_notpickable-mixed_hover";
            icon = (Texture2D)EditorGUIUtility.IconContent(iconName).image;
            tooltip = $"Drake Renderer hack authoring mode. Hack: {(SceneAuthoringHack ? "On" : "Off")}";
        }
    }
}
