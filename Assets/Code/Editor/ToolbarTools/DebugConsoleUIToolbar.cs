using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.UI;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.Utility.UI;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using World = Awaken.TG.MVC.World;

namespace Awaken.TG.Editor.ToolbarTools {
    [EditorToolbarElement(ID, typeof(SceneView))]
    public sealed class DebugConsoleUIToolbar : EditorToolbarToggle {
        public const string ID = "DebugConsoleUI";
        static bool s_isEnabled;
        
        public DebugConsoleUIToolbar() {
            text = "Console UI";
            tooltip = "Toggles debug mode to test console dependent UI f.e. font size";
            this.RegisterValueChangedCallback(HandleToggleValue);
            
            s_isEnabled = EditorPrefs.GetBool(ID, false);
            PlatformUtils.sDebugConsolePlatform = s_isEnabled;
            SetValueWithoutNotify(s_isEnabled);
        }
        
        static void HandleToggleValue(ChangeEvent<bool> value) {
            EditorPrefs.SetBool(ID, value.newValue);
            s_isEnabled = value.newValue;
            PlatformUtils.sDebugConsolePlatform = s_isEnabled;

            if (Application.isPlaying) {
                World.Only<ConsoleUISetting>().SetEnabled(s_isEnabled);
                VCDeviceFontAdjuster.DebugSetAll();
                UTKPanelSettingsService.DebugConsole();
                VCDeviceRectAdjuster.DebugSetAll();
                VBlurBackground.DebugConsole();
            }
        }
    }
}