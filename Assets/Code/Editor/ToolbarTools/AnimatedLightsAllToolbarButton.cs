using Awaken.TG.Editor.Assets;
using Awaken.TG.Graphics.VFX;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Awaken.TG.Editor.ToolbarTools {
    [EditorToolbarElement(ID, typeof(SceneView))]
    public sealed class AnimatedLightsAllToolbarButton : EditorToolbarToggle {
        public const string ID = "X/5";

        public AnimatedLightsAllToolbarButton() {
            icon = (Texture2D)EditorGUIUtility.IconContent("d_PreMatLight1@2x").image;
            tooltip = "Toggles all dynamic lights updates";
            value = ProjectValidator.DynamicLightsAllActivePref;
            
            LightController.EditorPreviewAllUpdates = value;
        }

        protected override void ToggleValue() {
            base.ToggleValue();
            ProjectValidator.DynamicLightsAllActivePref = value;
            LightController.EditorPreviewAllUpdates = value;
        }
    }
}