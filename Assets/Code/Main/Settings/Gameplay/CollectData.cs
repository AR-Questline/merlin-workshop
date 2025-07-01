using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class CollectData : Setting {
        
        ToggleOption _toggle;
        
        public sealed override string SettingName => LocTerms.SettingsCollectData.Translate();
        public override bool IsVisible => !PlatformUtils.IsConsole;
        public bool Enabled => _toggle?.Enabled ?? true; 
        
        public override IEnumerable<PrefOption> Options => _toggle.Yield();
        
        public CollectData() {
            _toggle = new ToggleOption("Setting_CollectData", SettingName, true, true);
            _toggle.AddTooltip(LocTerms.CollectDataSettingTooltip.Translate);
        }

        public void Disable() {
            if (_toggle == null) {
                return;
            }
            _toggle.Enabled = false;
            Apply(out _);
        }

        public override void RestoreDefault() {
            // We can't change this setting without explicit player's action/consent.
        }
    }
}