using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class VignetteSetting : Setting, IGraphicSetting {
        const bool EnabledByDefault = true;
        const string VignetteEnabledPrefId = "Vignette";

        // === Options
        readonly ToggleOption _vignetteToggle;
        
        public sealed override string SettingName => LocTerms.SettingsVignette.Translate();
        public bool Enabled => _vignetteToggle.Enabled;

        public override IEnumerable<PrefOption> Options => _vignetteToggle.Yield();

        public IEnumerable<Preset> MatchingPresets => Preset.AllPredefined;

        // === Initialization
        public VignetteSetting() {
            _vignetteToggle = new ToggleOption(VignetteEnabledPrefId, SettingName, EnabledByDefault, true);
        }

        public void SetValueForPreset(Preset preset) {
            _vignetteToggle.Enabled = EnabledByDefault;
        }
    }
}