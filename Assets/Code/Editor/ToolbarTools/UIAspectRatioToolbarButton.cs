using Awaken.TG.Main.Utility.UI;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Awaken.TG.Editor.ToolbarTools {
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class UIAspectRatioToolbarButton : EditorToolbarButton {
        public const string ID = "UIAspectRatioAdjusterButton";

        public UIAspectRatioToolbarButton() {
            text = "Refresh aspect ratio";
            tooltip = "Refreshes canvas scaler match with current aspect ratio for all canvases";
            clicked += OnClick;
        }

        static void OnClick() {
            if (Application.isPlaying) {
                VCAspectRatioScaler.DebugRefresh();
            } else {
                Log.Minor?.Warning($"{nameof(UIAspectRatioToolbarButton)} is only available in play mode.");
            }
        }
    }
}