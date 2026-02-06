using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Utility;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Windows {
    public class SettingsTabType : RichEnum {
        readonly string _locTerm;
        readonly Func<SettingsMaster, IEnumerable<ISetting>> _getSettings;

        public string DisplayName => _locTerm?.Translate() ?? "Debug";

        [UnityEngine.Scripting.Preserve] public static readonly SettingsTabType
            GameplaySettings = new SettingsTabType(nameof(GameplaySettings), LocTerms.SettingsGameplayTab, master => master.GameplaySettings),
            AudioSettings = new SettingsTabType(nameof(AudioSettings), LocTerms.SettingsAudioTab, master => master.AudioSettings),
            DisplaySettings = new SettingsTabType(nameof(DisplaySettings), LocTerms.SettingsDisplayTab, master => master.DisplaySettings),
            GraphicSettings = new SettingsTabType(nameof(GraphicSettings), LocTerms.SettingsGraphicTab, master => master.GraphicSettings),
            ControlsSettings = new SettingsTabType(nameof(ControlsSettings), LocTerms.SettingsControlsTab, master => master.KeyboardControlsSettings),
            GamepadControlsSettings = new SettingsTabType(nameof(GamepadControlsSettings), LocTerms.SettingsControlsTab, master => master.GamepadControlsSettings
                .Where(s => s.controllerIdentifier == RewiredHelper.CurrentHardwareTypeGuid)),
            GeneralSettings = new SettingsTabType(nameof(GeneralSettings), LocTerms.ItemsTabOther, master => master.GeneralSettings),
            AccessibilitySettings = new SettingsTabType(nameof(AccessibilitySettings), LocTerms.SettingsAccessibilityTab, master => master.AccessibilitySettings),
            DlcSettings = new SettingsTabType(nameof(DlcSettings), LocTerms.SettingsDlcTab, master => master.DlcSettings),
            DebugSettings = new SettingsTabType(nameof(DebugSettings), null, master => null);

        SettingsTabType(string enumName, string locTerm, Func<SettingsMaster, IEnumerable<ISetting>> getSettings) : base(enumName) {
            _locTerm = locTerm;
            _getSettings = getSettings;
        }
        
        public IEnumerable<ISetting> GetSettings(SettingsMaster master) => _getSettings(master);
    }
}
