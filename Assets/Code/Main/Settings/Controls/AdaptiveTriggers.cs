using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Controls {
    public partial class AdaptiveTriggers : Setting, IRewiredSetting {
        ToggleOption _option;

        public sealed override string SettingName => PlatformUtils.IsPS5 || RewiredHelper.IsDualSense
            ? LocTerms.SettingsAdaptiveTriggers.Translate()
            : LocTerms.SettingsAdaptiveTriggersMicrosoft.Translate();
        
        public override IEnumerable<PrefOption> Options => RewiredHelper.IsGamepad ? _option.Yield() : Enumerable.Empty<PrefOption>();
        public bool Enabled => _option.Enabled;

        public AdaptiveTriggers() {
            _option = new ToggleOption("Settings_AdaptiveTriggers", SettingName, true, true);
        }
    }
}