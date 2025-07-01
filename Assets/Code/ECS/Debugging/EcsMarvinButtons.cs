using Awaken.Utility.Debugging;
using Awaken.Utility.UI;

namespace Awaken.ECS.Debugging {
    // ReSharper disable once UnusedType.Global
    internal static class EcsMarvinButtons {
        [StaticMarvinButton(state: nameof(IsSystemsShown))]
        static void ShowSystems() {
            DebugSystemsWindow.Toggle(UGUIWindowUtils.WindowPosition.TopLeft);
        }

        static bool IsSystemsShown() => DebugSystemsWindow.IsShown;
    }
}
