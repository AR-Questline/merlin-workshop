using Awaken.TG.Editor.Assets;
using Awaken.TG.Graphics.VFX;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Awaken.TG.Editor.ToolbarTools {
    [EditorToolbarElement(ID, typeof(SceneView))]
    public sealed class AnimatedLightToolbarButton : EditorToolbarToggle {
        public const string ID = "X/3";

        public AnimatedLightToolbarButton() {
            icon = (Texture2D)EditorGUIUtility.IconContent("AutoLightbakingOn@2x").image;
            tooltip = "Toggles selected dynamic lights updates";
            value = ProjectValidator.DynamicLightsActivePref;
            
            LightController.EditorPreviewUpdates = value;
            DecalController.EditorPreviewUpdates = value;
        }

        protected override void ToggleValue() {
            base.ToggleValue();
            ProjectValidator.DynamicLightsActivePref = value;
            LightController.EditorPreviewUpdates = value;
            DecalController.EditorPreviewUpdates = value;
        }
    }
}