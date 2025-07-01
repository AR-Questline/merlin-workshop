using System.Collections.Generic;
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Graphics;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class Shadows : Setting, IGraphicSetting {
        const string ShadowsEnabledPrefId = "ShadowsEnabled";
        const string ShadowsDistancePrefId = "ShadowsDistance";
        const string ContactShadowsEnabledPrefId = "ContactShadowsEnabled";

        // === Options
        readonly DependentOption _mainOption;

        readonly ToggleOption _shadowsToggle;
        readonly SliderOption _shadowsDistance;
        
        readonly ToggleOption _contactShadowsToggle;
        
        readonly Preset[] _allowedForContactShadowPresets = { Preset.Ultra, Preset.High };
        
        public sealed override string SettingName => LocTerms.SettingsShadows.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole || CheatController.CheatsEnabled();
        public override IEnumerable<PrefOption> Options => _mainOption.Yield();

        public bool ShadowsEnabled => _shadowsToggle.Enabled;
        public float ShadowsDistance => _shadowsDistance.Value;
        public bool ContactShadowsEnabled => ShadowsEnabled && _contactShadowsToggle.Enabled;

        public IEnumerable<Preset> MatchingPresets {
            get {
                if (ShadowsEnabled) {
                    if (ContactShadowsEnabled) {
                        yield return Preset.High;
                        yield return Preset.Ultra;
                    } else if (ShadowsDistance > 0.5f) {
                        yield return Preset.Medium;
                    } else {
                        yield return Preset.Low;
                    }
                }
            }
        }

        // === Initialization
        public Shadows() {
            _shadowsToggle = new(ShadowsEnabledPrefId, SettingName, true, false);
            _shadowsDistance = new(ShadowsDistancePrefId, LocTerms.SettingsShadowsDistance.Translate(), 0, 1, false,
                NumberWithPercentFormat, 1, false);

            _contactShadowsToggle = new(ContactShadowsEnabledPrefId, LocTerms.SettingsContactShadows.Translate(), true,
                false);
            _contactShadowsToggle.AddTooltip(static () => LocTerms.SettingsTooltipContactShadows.Translate());
            
            var contactShadowsComposition = new DependentOption(_contactShadowsToggle);
            _mainOption = new(_shadowsToggle, _shadowsDistance, contactShadowsComposition);
        }

        // === Logic
        public void SetValueForPreset(Preset preset) {
            _shadowsToggle.Enabled = true;
            if (preset.QualityIndex == Preset.Low.QualityIndex) {
                _shadowsDistance.Value = 0.35f;
            } else if (preset.QualityIndex == Preset.Medium.QualityIndex) {
                _shadowsDistance.Value = 0.6f;
            } else if (preset.QualityIndex == Preset.High.QualityIndex) {
                _shadowsDistance.Value = 0.85f;
            } else {
                _shadowsDistance.Value = 1f;
            }
            
            _contactShadowsToggle.Enabled = preset.IsIn(_allowedForContactShadowPresets);
        }

        protected override void OnApply() {
            Set();
        }

        void Set() {
            SmallObjectsShadows.ChangedShadowsBias(ShadowsDistance);
        }
    }
}
