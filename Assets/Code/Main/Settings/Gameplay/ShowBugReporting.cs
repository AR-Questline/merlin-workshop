using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Settings.Windows;
using Awaken.TG.Main.UI.Bugs;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class ShowBugReporting : Setting {
        ButtonOption _button;
        
        public sealed override string SettingName => LocTerms.SettingsBugReporting.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole;

        public override IEnumerable<PrefOption> Options => _button.Yield();

        public ShowBugReporting() {
            _button = new ButtonOption("Setting_BugReportingButton", SettingName, ShowBugReportingUI);
        }

        void ShowBugReportingUI() {
            if (PlatformUtils.IsConsole) {
                return;
            }
            
            World.Any<AllSettingsUI>()?.Discard();
            World.Add(new UserBugReporting(true));
        }
    }
}