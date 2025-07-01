using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class FontSizeSetting : Setting {
        const string PrefId = "FontSizePreset";
        
        public EnumArrowsOption EnumOption { get; }
        readonly List<ToggleOption> _toggleOptions = new();
        readonly ListDictionary<ToggleOption, FontSize> _presetByOption = new();
        
        public sealed override string SettingName => LocTerms.SettingsFontSize.Translate();
        public override IEnumerable<PrefOption> Options => EnumOption.Yield();
        public FontSize ActiveFontSize => _presetByOption[EnumOption.Option];
        
        public FontSizeSetting() {
            foreach (FontSize preset in RichEnum.AllValuesOfType<FontSize>()) {
                var option = new ToggleOption(GetPresetId(preset), preset.DisplayName, preset == FontSize.Medium, true); 
                _toggleOptions.Add(option);
                _presetByOption.Add(option, preset);
            }

            ToggleOption defaultOption = _toggleOptions.FirstOrDefault(o => o.DefaultValue);
            EnumOption = new EnumArrowsOption(PrefId, SettingName, defaultOption, true, _toggleOptions.ToArray());
        }

        public string GetPresetId(FontSize preset) {
            return $"{PrefId}_{preset.EnumName}";
        }
        
        public void SetFontOption(FontSize fontSize){
            int targetFont = _presetByOption.IndexOfValue(fontSize);
            EnumOption.Option = _toggleOptions[targetFont];
        }

        public void SetForbiddenOption(FontSize fontSize) {
            int targetFont = _presetByOption.IndexOfValue(fontSize);
            EnumOption.SetForbiddenOptions(_toggleOptions[targetFont]);
        }
    }
}
