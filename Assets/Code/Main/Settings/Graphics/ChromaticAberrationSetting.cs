using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    public partial class ChromaticAberrationSetting : Setting, IGraphicSetting {
        const bool EnabledByDefault = true;
        const string PrefId = "ChromaticAberration";

        // === Options
        ToggleOption Option { get; }
        
        public sealed override string SettingName => LocTerms.SettingsChromaticAberration.Translate();
        public bool Enabled => Option.Enabled;

        public override IEnumerable<PrefOption> Options => Option.Yield();
        public IEnumerable<Preset> MatchingPresets => Preset.AllPredefined;

        // === Initialization
        public ChromaticAberrationSetting() {
            Option = new ToggleOption(PrefId, SettingName, EnabledByDefault, false);
        }

        public void SetValueForPreset(Preset preset) {
            Option.Enabled = EnabledByDefault;
        }
    }
}