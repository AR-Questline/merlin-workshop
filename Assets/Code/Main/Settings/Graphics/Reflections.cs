using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Graphics {
    /// <summary>
    /// It's not visible, but it's there in case we start using Reflection Probes 
    /// </summary>
    public partial class Reflections : Setting, IGraphicSetting {
        const string PrefId = "Reflections";

        // === Options
        ToggleOption Option { get; }
        
        public sealed override string SettingName => LocTerms.SettingsReflections.Translate();
        public override bool IsVisible => false;
        public bool Enabled => Option.Enabled;

        public override IEnumerable<PrefOption> Options => Option.Yield();

        public IEnumerable<Preset> MatchingPresets => Preset.AllPredefined;

        // === Initialization
        public Reflections() {
            Option = new ToggleOption(PrefId, SettingName, true, false);
        }
        
        public void SetValueForPreset(Preset preset) {
            Option.Enabled = true;
        }
    }
}
