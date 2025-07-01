using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Controls {
    public partial class PadVibrations : Setting, IRewiredSetting {
        ToggleOption _option;
        
        public sealed override string SettingName => PlatformUtils.IsMicrosoft 
            ? LocTerms.SettingsPadVibrationsMicrosoft.Translate()
            : LocTerms.SettingsPadVibrations.Translate();
        
        public override IEnumerable<PrefOption> Options => RewiredHelper.IsGamepad ? _option.Yield() : Enumerable.Empty<PrefOption>();

        public bool Enabled => _option.Enabled;

        public PadVibrations() {
            _option = new ToggleOption("Settings_PadVibrations", SettingName, true, true);
        }
    }
}