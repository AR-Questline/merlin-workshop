using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Options;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Accessibility {
    public partial class FontChooseSetting : Setting, IRewiredSetting {
        const string PrefId = "Settings_FontChooseSetting";
        
        public EnumArrowsOption EnumOption { get; }
        readonly List<ToggleOption> _toggleOptions = new();
        readonly ListDictionary<ToggleOption, FontFamily> _presetByOption = new();

        public sealed override string SettingName => LocTerms.FontSetting.Translate();
        public override IEnumerable<PrefOption> Options => EnumOption.Yield();
        public FontFamily ActiveFont => _presetByOption[EnumOption.Option];
        
        public FontChooseSetting() {
            foreach (FontFamily preset in RichEnum.AllValuesOfType<FontFamily>()) {
                var option = new ToggleOption(GetPresetId(preset), preset.DisplayName, preset == FontFamily.Serif, true); 
                _toggleOptions.Add(option);
                _presetByOption.Add(option, preset);
            }

            ToggleOption defaultOption = _toggleOptions.FirstOrDefault(o => o.DefaultValue);
            EnumOption = new EnumArrowsOption(PrefId, SettingName, defaultOption, true, _toggleOptions.ToArray());

            if (LocalizationHelper.IsNonLatinaCharacters()) {
                SetForbiddenOption(FontFamily.Serif);
                SetFontOption(FontFamily.Sans);
            }
        }

        public string GetPresetId(FontFamily preset) {
            return $"{PrefId}_{preset.EnumName}";
        }
        
        public void SetFontOption(FontFamily font){
            int targetFont = _presetByOption.IndexOfValue(font);
            EnumOption.Option = _toggleOptions[targetFont];
        }
        
        public void SetForbiddenOption(FontFamily font) {
            int targetFont = _presetByOption.IndexOfValue(font);
            EnumOption.SetForbiddenOptions(_toggleOptions[targetFont]);
        }
    }
}