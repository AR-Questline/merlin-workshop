using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class ScreenShakesReactiveSetting : Setting {
        readonly ToggleOption _toggle;
        
        public sealed override string SettingName => LocTerms.SettingsScreenShakeReactive.Translate();
        public bool Enabled => _toggle?.Enabled ?? true;
        public override IEnumerable<PrefOption> Options => _toggle?.Yield();

        public ScreenShakesReactiveSetting() {
            _toggle = new ToggleOption("Accessibility_ScreenShakesReactive_Allowed", SettingName, true, true);
            _toggle.AddTooltip(LocTerms.ScreenShakeReactiveSettingTooltip.Translate);
        }
        
        public void SetToggle(bool enabled) {
            _toggle.Enabled = enabled;
        }
    }
}
