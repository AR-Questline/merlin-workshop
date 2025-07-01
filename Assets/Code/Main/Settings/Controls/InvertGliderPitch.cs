using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Controls {
    public partial class InvertGliderPitch : Setting {
        
        ToggleOption _invert;
        
        public sealed override string SettingName => LocTerms.SettingsGliderPitchInvert.Translate();
        public override IEnumerable<PrefOption> Options => _invert.Yield();
        public bool Invert => _invert.Enabled;
        
        public InvertGliderPitch() {
            _invert = new("Settings_GliderPitchInvert", SettingName, false, true);
            _invert.AddTooltip(LocTerms.InvertedGlidingPitchSettingTooltip.Translate);
        }
    }
}