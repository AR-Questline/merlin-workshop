using System.Collections.Generic;
using Awaken.TG.Main.AudioSystem.Biomes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Audio {
    public partial class InfluencerMode : Setting {
        
        ToggleOption _toggle;
        
        public sealed override string SettingName => LocTerms.SettingsInfluencerMode.Translate();
        public bool Enabled => _toggle.Enabled;

        public override IEnumerable<PrefOption> Options => _toggle.Yield();
        protected override bool AutoApplyOnInit => false;

        public InfluencerMode() {
            _toggle = new ToggleOption("Setting_InfluencerMode", SettingName, false, true);
        }

        protected override void OnApply() {
            World.Services.Get<AudioCore>().ResetMusic();
        }
    }
}