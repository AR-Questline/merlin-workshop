using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.Utility.UI;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using World = Awaken.TG.MVC.World;

namespace Awaken.TG.Editor.ToolbarTools {
    [EditorToolbarElement(ID, typeof(SceneView))]
    public class DebugConsoleUIToolbar : EditorToolbarToggle {
        public const string ID = "DebugConsoleUI";
        
        public DebugConsoleUIToolbar() {
            text = "Console UI";
            tooltip = "Toggles debug mode to test console dependent UI f.e. font size";
            this.RegisterValueChangedCallback(HandleToggleValue);
        }
        
        static void HandleToggleValue(ChangeEvent<bool> value) {
            if (Application.isPlaying) {
                bool isConsoleActive = value.newValue;
                World.Only<ConsoleUISetting>().SetEnabled(value.newValue);
                VCDeviceFontAdjuster.DebugSetAll(isConsoleActive);
                UTKPanelSettingsService.DebugConsole(isConsoleActive);
                VCDeviceRectAdjuster.DebugSetAll(isConsoleActive);
            } else {
                Log.Minor?.Warning($"{nameof(DebugConsoleUIToolbar)} is only available in play mode.");
            }
        }
    }
}