using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Controls {
    public partial class FOVChanges : Setting {
        ToggleOption _toggle;
        readonly SliderOption _multiplierSlider;
        readonly DependentOption _dependentOption;
        
        public sealed override string SettingName => LocTerms.SettingsEnableFOVChanges.Translate();
        
        public bool AreFOVChangesAllowed => _toggle?.Enabled ?? true;
        public override IEnumerable<PrefOption> Options => _dependentOption.Yield();
        public float FOVChangeMultiplier => _multiplierSlider.Value;

        public FOVChanges() {
            _toggle = new ToggleOption("Setting_FOVChangesAllowed", SettingName, true, true);
            _toggle.AddTooltip(LocTerms.FovChangesSettingTooltip.Translate);
            
            _multiplierSlider = new SliderOption("Settings_FOVMultiplier", LocTerms.SettingsFOVMultiplier.Translate(), 0.01f, 2f, false, NumberWithPercentFormat, 1f, true, 0.1f);
            _dependentOption = new DependentOption(_toggle, _multiplierSlider);
        }
    }
}