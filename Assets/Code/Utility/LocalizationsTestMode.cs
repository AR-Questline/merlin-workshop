#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.TG.Utility {
    public static class LocalizationsTestMode {
        const string TestModeMenuName = "TG/Localization/Enable Test Mode";
        public static bool Enabled {
            get => SafeEditorPrefs.GetInt("LocalizationsTestMode", 0) == 1;
            [UnityEngine.Scripting.Preserve] private set => SafeEditorPrefs.SetInt("LocalizationsTestMode", value ? 1 : 0);
        }

#if UNITY_EDITOR
        // Disabled as we now enable test mode in playmode by default
        // [MenuItem(TestModeMenuName)]
        // public static void TestMode() {
        //     Enabled = !Menu.GetChecked(TestModeMenuName);
        // }
        //
        // [MenuItem(TestModeMenuName, true)]
        // public static bool TestModeVal() {
        //     Menu.SetChecked(TestModeMenuName, Enabled);
        //     return true;
        // }
#endif
    }
}