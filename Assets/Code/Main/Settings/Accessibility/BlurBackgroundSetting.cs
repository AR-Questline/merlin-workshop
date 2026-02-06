using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class BlurBackgroundSetting : Setting {
        ToggleOption _toggle;
        
        public sealed override string SettingName => LocTerms.SettingsBlurBackground.Translate();
        public bool Enabled => _toggle.Enabled;
        
        public override IEnumerable<PrefOption> Options => _toggle.Yield();

        public BlurBackgroundSetting() {
            _toggle = new ToggleOption("Setting_BlurBackground", SettingName, true, true);
        }
    }
}