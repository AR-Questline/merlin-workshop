using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Utility;
using Awaken.Utility.Enums;
using UnityEngine.TextCore.Text;

namespace Awaken.TG.Main.Settings.Accessibility {
    public class FontFamily : RichEnum {
        string NameKey { get; }
        public FontAsset FontAsset { get; set; }
        public string DisplayName => NameKey.Translate();

        [UnityEngine.Scripting.Preserve] public static readonly FontFamily
            Sans = new(nameof(Sans), LocTerms.MainSansFont.Translate(), CommonReferences.Get.SansFontAsset),
            Serif = new(nameof(Serif), LocTerms.MainSerifFont.Translate(), CommonReferences.Get.SerifFontAsset);

        public FontFamily(string enumName, string nameKey, FontAsset fontAsset) : base(enumName) {
            NameKey = nameKey;
            FontAsset = fontAsset;
        }
    }
}
