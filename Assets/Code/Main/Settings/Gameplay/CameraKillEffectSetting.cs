using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class CameraKillEffectSetting : Setting {
        const string PrefId = "Setting_CameraKillEffect";
        
        ToggleOption _toggle;
        ToggleOption _stealthToggle;
        ToggleOption _criticalToggle;
        ToggleOption _weakspotToggle;
        readonly DependentOption _dependentOption;
        
        public sealed override string SettingName => LocTerms.SettingCameraKillEffect.Translate();
        public override IEnumerable<PrefOption> Options => _dependentOption.Yield();
        public bool Enabled => _toggle.Enabled;
        public bool StealthEnabled => _stealthToggle.Enabled;
        public bool CriticalEnabled => _criticalToggle.Enabled;
        public bool WeakspotEnabled => _weakspotToggle.Enabled;
        
        public CameraKillEffectSetting() {
            _toggle = new ToggleOption(PrefId, SettingName, true, true);
            _toggle.AddTooltip(LocTerms.CameraKillEffectSettingTooltip.Translate);
            _stealthToggle = new ToggleOption($"{PrefId}Stealth", LocTerms.SettingStealthKillCamera.Translate(), true, true);
            _criticalToggle = new ToggleOption($"{PrefId}Critical", LocTerms.SettingCriticalKillCamera.Translate(), true, true);
            _weakspotToggle = new ToggleOption($"{PrefId}Weakspot", LocTerms.SettingWeakspotKillCamera.Translate(), true, true);
            _dependentOption = new DependentOption(_toggle, _stealthToggle, _criticalToggle, _weakspotToggle);
        }
    }
}