using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class QuickUseWheelSetting : Setting {
        const string PrefId = "Setting_QuickUseWheel";
        
        readonly ToggleOption _hold;
        readonly EnumArrowsOption _options;
        
        public override IEnumerable<PrefOption> Options => _options.Yield();
        public bool HoldEnabled => _options.Option == _hold;
        public sealed override string SettingName => LocTerms.SettingsQuickUseWheel.Translate();

        public QuickUseWheelSetting() {
            var toggle = new ToggleOption($"{PrefId}_Toggle", LocTerms.SettingsBindingToggle.Translate(), false, true);
            _hold = new ToggleOption($"{PrefId}_Hold", LocTerms.SettingsBindingHold.Translate(), true, true);
            _options = new EnumArrowsOption(PrefId, SettingName, _hold, true, _hold, toggle);
        }
    }
}