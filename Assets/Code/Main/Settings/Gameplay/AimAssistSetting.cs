using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Gameplay {
    public partial class AimAssistSetting : Setting {
        const string PrefId = "AimAssist";
        
        EnumArrowsOption Option { get; }

        public sealed override string SettingName => PlatformUtils.IsMicrosoft 
            ? LocTerms.SettingsAimAssistMicrosoft.Translate()
            : LocTerms.SettingsAimAssist.Translate();
        
        public bool Enabled => Option.Option.ID != $"{PrefId}_{AimAssistType.Off.EnumName}"; 
        public bool HighAssistEnabled => Option.Option.ID == $"{PrefId}_{AimAssistType.High.EnumName}"; 
        
        public override IEnumerable<PrefOption> Options => RewiredHelper.IsGamepad ? Option.Yield() : Enumerable.Empty<PrefOption>();
        readonly List<ToggleOption> _toggleOptions = new();

        public AimAssistSetting() {
            foreach (AimAssistType preset in RichEnum.AllValuesOfType<AimAssistType>()) {
                var option = new ToggleOption($"{PrefId}_{preset.EnumName}", preset.DisplayName, preset == AimAssistType.Low, false);
                _toggleOptions.Add(option);
            }
            
            ToggleOption defaultOption = _toggleOptions.FirstOrDefault(o => o.DefaultValue);
            Option = new EnumArrowsOption(PrefId, SettingName, defaultOption, false, _toggleOptions.ToArray());
        }
    }
    
    public class AimAssistType : RichEnum {
        readonly string _nameKey;
        public string DisplayName => _nameKey.Translate();
        
        [UnityEngine.Scripting.Preserve] 
        public static readonly AimAssistType
            Off = new(nameof(Off), LocTerms.Off),
            Low = new(nameof(Low), LocTerms.SettingsPresetLow),
            High = new(nameof(High), LocTerms.SettingsPresetHigh);

        AimAssistType(string enumName, string nameKey) : base(enumName) {
            _nameKey = nameKey;
        }
    }
}