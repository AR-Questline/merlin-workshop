using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class MotionBlurSetting : Setting, IGraphicSetting {
        const bool EnabledByDefault = true;
        const float DefaultBlurIntensity = 0.333f;
        const string BlurEnabledPrefId = "MotionBlur";
        const string BlurIntensityPrefId = "MotionBlurIntensity";

        // === Options
        readonly DependentOption _mainOption;
        readonly ToggleOption _blurToggle;
        readonly SliderOption _blurIntensity;
        
        public sealed override string SettingName => LocTerms.SettingsMotionBlur.Translate();
        public bool Enabled => _blurToggle.Enabled;
        public float Intensity => _blurIntensity.Value;

        public override IEnumerable<PrefOption> Options => _mainOption.Yield();

        public IEnumerable<Preset> MatchingPresets => Preset.AllPredefined;

        // === Initialization
        public MotionBlurSetting() {
            _blurToggle = new ToggleOption(BlurEnabledPrefId, SettingName, EnabledByDefault, false);
            _blurIntensity = new(BlurIntensityPrefId, LocTerms.SettingsMotionBlurIntensity.Translate(), 0, 1, false,
                NumberWithPercentFormat, DefaultBlurIntensity, false);
            
            _mainOption = new DependentOption(_blurToggle, _blurIntensity);
        }

        public void SetValueForPreset(Preset preset) {
            _blurToggle.Enabled = EnabledByDefault;
            _blurIntensity.Value = DefaultBlurIntensity;
        }
    }
}