using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.LeshyRenderer {
    public class LeshyDebugWindow : UGUIWindowDisplay<LeshyDebugWindow> {
        protected override string Title => "Leshy";
        protected override bool WithSearch => false;

        protected override void DrawWindow() {
            var leshy = World.Services.Get<LeshyManager>();
            leshy.enabled = GUILayout.Toggle(leshy.enabled, "Enabled");
            leshy.EnabledRendering = GUILayout.Toggle(leshy.EnabledRendering, "Enabled rendering");
            leshy.EnabledCells = GUILayout.Toggle(leshy.EnabledCells, "Enabled cells");
            leshy.EnabledCollider = GUILayout.Toggle(leshy.EnabledCollider, "Enabled colliders");
            leshy.EnabledLoading = GUILayout.Toggle(leshy.EnabledLoading, "Enabled loading");
        }

        [StaticMarvinButton(state: nameof(IsLeshyWindowShown))]
        static void ShowLeshyWindow() {
            var leshy = World.Services.TryGet<LeshyManager>();
            if (leshy == null) {
                return;
            }
            LeshyDebugWindow.Toggle(UGUIWindowUtils.WindowPosition.BottomLeft);
        }

        static bool IsLeshyWindowShown() {
            return World.Services.Has<LeshyManager>() && LeshyDebugWindow.IsShown;
        }
    }
}
