using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.FirstTime;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class ConsoleUISetting : Setting, IRewiredSetting {
        public ToggleOption Option { get; }
        public sealed override string SettingName => LocTerms.SettingsConsoleUI.Translate();
        public override IEnumerable<PrefOption> Options => Option.Yield();
        public bool Enabled => Option.Enabled;
        
        public void SetEnabled(bool enabled) {
            Option.Enabled = enabled;
        }

        public ConsoleUISetting() {
            Option = new ToggleOption("Settings_ConsoleUISetting", SettingName, PlatformUtils.IsConsole || PlatformUtils.IsSteamDeck, true);
            Option.onChange += UpdateFontSizeSetting;
        }

        static void UpdateFontSizeSetting(bool enabled) {
            var fontSizeSetting = World.Only<FontSizeSetting>();
            
            if (enabled) {
                if (fontSizeSetting.ActiveFontSize == FontSize.Huge && World.Any<FirstTimeSettings>() == false) {
                    fontSizeSetting.SetFontOption(FontSize.Big);
                }
                
                fontSizeSetting.SetForbiddenOption(FontSize.Huge);
            } else {
                fontSizeSetting.EnumOption.SetForbiddenOptions();
            }
        }
    }
}