using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Unity.Mathematics;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class ContrastSetting : Setting {
        public const float SliderStepChange = 0.1f;
        public const float MinValue = 0f;
        public const float MaxValue = 1f;
        const string PrefKey = "ContrastSetting";

        // === Options
        SliderOption Option { get; }
        
        public sealed override string SettingName => LocTerms.SettingsContrast.Translate();
        public override IEnumerable<PrefOption> Options => Option.Yield();
        public float Value => Option.Value;

        // === Initialization
        public ContrastSetting() {
            Option = new SliderOption(PrefKey, SettingName, MinValue, MaxValue, false, "{0:P0}", 0.5f, false, SliderStepChange);
        }
    }
}
