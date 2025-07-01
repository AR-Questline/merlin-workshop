using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.FirstTime {
    public partial class ScreenShakesAllInOneSetting : Setting {
        const string PrefId = "ScreenShakesAll";
        readonly List<ToggleOption> _toggleOptions = new();
        
        public sealed override string SettingName => LocTerms.SettingsScreenShakeAll.Translate();
        public override IEnumerable<PrefOption> Options => EnumOption.Yield();
        EnumArrowsOption EnumOption { get; }
        
        public ScreenShakesAllInOneSetting() {
            foreach (ScreenShakesType preset in RichEnum.AllValuesOfType<ScreenShakesType>()) {
                var option = new ToggleOption($"{PrefId}_{preset.EnumName}", preset.DisplayName, preset == ScreenShakesType.ProactiveReactive, true); 
                _toggleOptions.Add(option);
            }

            ToggleOption defaultOption = _toggleOptions.FirstOrDefault(option => option.DefaultValue);
            EnumOption = new EnumArrowsOption(PrefId, SettingName, defaultOption, true, _toggleOptions.ToArray());
        }

        protected override void OnApply() {
            if (EnumOption.Option.ID == $"{PrefId}_{ScreenShakesType.Proactive.EnumName}") {
                ApplySettings(true, false);
            } else if (EnumOption.Option.ID == $"{PrefId}_{ScreenShakesType.ProactiveReactive.EnumName}") {
                ApplySettings(true, true);
            } else {
                ApplySettings(false, false);
            }
        }

        static void ApplySettings(bool proactive, bool reactive) {
            World.Only<ScreenShakesProactiveSetting>().SetToggle(proactive);
            World.Only<ScreenShakesReactiveSetting>().SetToggle(reactive);
        }
    }
    
    public class ScreenShakesType : RichEnum {
        readonly string _nameKey;
        public string DisplayName => _nameKey.Translate();

        [UnityEngine.Scripting.Preserve] 
        public static readonly ScreenShakesType
            Off = new(nameof(Off), LocTerms.Off),
            Proactive = new(nameof(Proactive), LocTerms.SettingsScreenShakeAllProactive),
            ProactiveReactive = new(nameof(ProactiveReactive), LocTerms.SettingsScreenShakeAllProactiveAndReactive);
        
        ScreenShakesType(string enumName, string nameKey) : base(enumName) {
            _nameKey = nameKey;
        }
    }
}