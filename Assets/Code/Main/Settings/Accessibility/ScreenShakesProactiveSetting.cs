using System.Collections.Generic;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class ScreenShakesProactiveSetting : Setting {

        float _intensity;
        readonly ToggleOption _toggle;
        
        public sealed override string SettingName => LocTerms.SettingsScreenShakeProactive.Translate();
        public bool Enabled => _toggle?.Enabled ?? true;
        public float Intensity => Enabled ? _intensity : 0;

        public override IEnumerable<PrefOption> Options => _toggle?.Yield();

        public ScreenShakesProactiveSetting() {
            _toggle = new ToggleOption("Accessibility_ScreenShakesProactive_Allowed", SettingName, true, true);
            _toggle.AddTooltip(LocTerms.ScreenShakeProactiveSettingTooltip.Translate);
            _intensity = GameConstants.Get.screenShakeAnimationLayerWeight;
        }
        
        public void SetToggle(bool enabled) {
            _toggle.Enabled = enabled;
        }

        public void Debug_SetIntensity(float intensity) {
            this._intensity = intensity;
        }
    }
}