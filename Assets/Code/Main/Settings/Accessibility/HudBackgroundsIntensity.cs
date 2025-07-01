using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class HudBackgroundsIntensity : Setting {
        
        ToggleOption _toggle;
        readonly SliderOption _slider;
        readonly DependentOption _dependentOption;
        
        public sealed override string SettingName => LocTerms.HudBackground.Translate();
        public override IEnumerable<PrefOption> Options => _dependentOption.Yield();
        public bool AreHudBackgroundsAllowed => _toggle.Enabled;
        public float Value => AreHudBackgroundsAllowed ? _slider.Value : 0f;
        
        public HudBackgroundsIntensity(float defaultBackgroundIntensity = 0.5f) {
            _toggle = new ToggleOption("Setting_HudBackgroundAllowed", SettingName, false, true);
            _slider = new SliderOption("Setting_HudBackgroundsIntensity", LocTerms.HudBackgroundOpacity.Translate(), 0.1f, 1f, false, NumberWithPercentFormat, defaultBackgroundIntensity, true, 0.1f);
            _dependentOption = new DependentOption(_toggle, _slider);
        }
    }
}
