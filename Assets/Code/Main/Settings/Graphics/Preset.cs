using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Graphics {
    public class Preset : RichEnum {
        public int QualityIndex { get; }
        string NameKey { get; }
        
        public string DisplayName => NameKey.Translate();

        public static readonly Preset
            Low = new(nameof(Low), 0, LocTerms.SettingsPresetLow),
            Medium = new(nameof(Medium), 1, LocTerms.SettingsPresetMedium),
            High = new(nameof(High), 2, LocTerms.SettingsPresetHigh),
            Ultra = new(nameof(Ultra), 3, LocTerms.SettingsPresetUltra),
            Custom = new(nameof(Custom), -1, LocTerms.SettingsPresetCustom);
        
        Preset(string enumName, int qualityIndex, string nameKey) : base(enumName) {
            QualityIndex = qualityIndex;
            NameKey = nameKey;
        }

        public static readonly Preset[] AllPredefined = {
            Low, Medium, High, Ultra,
        };
    }
}