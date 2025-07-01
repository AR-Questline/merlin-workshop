using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Audio {
    public partial class DisableAudioInBackground : Setting {
        const bool DefaultOption = true;
        
        ToggleOption _toggle;
        
        public sealed override string SettingName => LocTerms.SettingsDisableAudioInBackground.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole;
        public bool Enabled => _toggle.Enabled;
        
        public override IEnumerable<PrefOption> Options => _toggle.Yield();

        public DisableAudioInBackground() {
            _toggle = new ToggleOption("Setting_DisableAudioInBackground", SettingName, DefaultOption, true);
            _toggle.AddTooltip(LocTerms.BackgroundAudioSettingTooltip.Translate);
        }
    }
}