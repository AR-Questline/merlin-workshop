using System.Collections.Generic;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class HeadBobbingSetting : Setting {
        public float Intensity => Enabled ? _intensity : 0;
        readonly ToggleOption _toggle;
        
        public sealed override string SettingName => LocTerms.HeadBobbingSetting.Translate();
        public override IEnumerable<PrefOption> Options => _toggle?.Yield();
        bool Enabled => _toggle?.Enabled ?? true;

        readonly float _intensity;
        
        public HeadBobbingSetting() {
            _toggle = new ToggleOption("Accessibility_HeadBobbing_Allowed", SettingName, true, true);
            _toggle.AddTooltip(LocTerms.HeadBobbingSettingTooltip.Translate);
            _intensity = GameConstants.Get.screenShakeAnimationLayerWeight;
        }
    }
}