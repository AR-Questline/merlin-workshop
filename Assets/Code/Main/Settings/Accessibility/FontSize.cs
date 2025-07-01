using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Accessibility {
    public class FontSize : RichEnum {
        public const int SmallChangeValue = -2;
        public const int MediumChangeValue = 0;
        public const int BigChangeValue = 2;
        public const int HugeChangeValue = 4;
        
        public int FontSizeChange { get; }
        string NameKey { get; }

        public string DisplayName => NameKey.Translate();
        
        [UnityEngine.Scripting.Preserve]
        public static readonly FontSize
            Small = new(nameof(Small), SmallChangeValue, LocTerms.FontSizeSmall),
            Medium = new(nameof(Medium), MediumChangeValue, LocTerms.FontSizeMedium),
            Big = new(nameof(Big), BigChangeValue, LocTerms.FontSizeBig),
            Huge = new(nameof(Huge), HugeChangeValue, LocTerms.FontSizeHuge);

        FontSize(string enumName, int fontSizeChange, string nameKey) : base(enumName) {
            FontSizeChange = fontSizeChange;
            NameKey = nameKey;
        }
    }
}
