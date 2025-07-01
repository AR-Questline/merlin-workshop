using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Automation;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class ShowTutorials : Setting {
        ToggleOption _toggle;
        
        public sealed override string SettingName => LocTerms.SettingsTutorials.Translate();
        public bool Enabled => _toggle.Enabled && !Automations.HasAutomation;
        
        public override IEnumerable<PrefOption> Options => _toggle.Yield();

        public ShowTutorials() {
            _toggle = new ToggleOption("Setting_Tutorials", SettingName, true, true);
        }
    }
}