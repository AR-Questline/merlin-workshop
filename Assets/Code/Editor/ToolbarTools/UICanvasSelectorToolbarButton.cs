using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Awaken.TG.Editor.ToolbarTools {
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class UICanvasSelectorToolbarButton : EditorToolbarButton {
        public const string ID = "X/2";

        public UICanvasSelectorToolbarButton() {
            text = "UI";
            tooltip = "Selects main UI canvas";
            clicked += OnClick;
        }

        static void OnClick() {
            if (Application.isPlaying) {
                Selection.activeGameObject = World.Services.Get<ViewHosting>().OnMainCanvas().gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
            } else {
                Log.Minor?.Warning($"{nameof(UICanvasSelectorToolbarButton)} is only available in play mode.");
            }
        }
    }
}