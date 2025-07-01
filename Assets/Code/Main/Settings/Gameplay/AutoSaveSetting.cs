using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class AutoSaveSetting : Setting {
        
        readonly ToggleOption _toggle;
        readonly DependentOption _dependentOption;
        readonly EnumArrowsOption _options;
        
        public override IEnumerable<PrefOption> Options => _dependentOption.Yield();
        public sealed override string SettingName => LocTerms.SettingsAutoSave.Translate();
        public string FrequencySettingName => LocTerms.SettingsAutoSaveFrequency.Translate();
        public bool Enabled => _toggle.Enabled;
        public float Interval => Frequency.Interval;
        AutoSaveFrequency Frequency => RichEnum.FromName<AutoSaveFrequency>(_options.Option.ID);
        
        static readonly ToggleOption OneMinute = new(nameof(AutoSaveFrequency.OneMinute), LocTerms.OneMinute.Translate(), false, true);
        static readonly ToggleOption ThreeMinutes = new(nameof(AutoSaveFrequency.ThreeMinutes), LocTerms.ThreeMinutes.Translate(), true, true);
        static readonly ToggleOption FiveMinutes = new(nameof(AutoSaveFrequency.FiveMinutes), LocTerms.FiveMinutes.Translate(), false, true);
        static readonly ToggleOption TenMinutes = new(nameof(AutoSaveFrequency.TenMinutes), LocTerms.TenMinutes.Translate(), false, true);

        public AutoSaveSetting() {
            _toggle = new ToggleOption("Setting_AutoSave", SettingName, true, true);
            _options = new EnumArrowsOption("Setting_AutoSaveFrequency", FrequencySettingName, ThreeMinutes, true, OneMinute, ThreeMinutes, FiveMinutes, TenMinutes);
            _dependentOption = new DependentOption(_toggle, _options);
        }
    }
}