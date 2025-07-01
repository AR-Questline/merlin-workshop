using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Controls {
    public partial class CameraSensitivity : Setting {
        SliderOption _slider;
        
        public sealed override string SettingName => LocTerms.SettingsCameraSensitivity.Translate();
        public override IEnumerable<PrefOption> Options => _slider.Yield();
        public float Sensitivity => _slider.Value;

        public CameraSensitivity() {
            _slider = new SliderOption("Settings_CameraSensitivity", SettingName, 0.1f, 4f, false, NumberWithPercentFormat, 1f, true, 0.1f);
        }
    }
}