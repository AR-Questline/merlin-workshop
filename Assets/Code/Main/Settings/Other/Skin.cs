using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Utility;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Settings.Other {
    public class Skin : RichEnum {
        public string Flag { get; }
        public ShareableSpriteReference Preview { get; }
        string NameKey { get; }
        public string DisplayName => NameKey.Translate();

        Skin(string enumName, string flag, string nameKey, ShareableSpriteReference preview) : base(enumName) {
            Flag = flag;
            NameKey = nameKey;
            Preview = preview;
        }

        public class QrkoSkin : Skin {
            QrkoSkin(string enumName, string flag, string nameKey, ShareableSpriteReference preview) : base(enumName, flag, nameKey, preview) { }

            [UnityEngine.Scripting.Preserve] public static readonly QrkoSkin
                Natural = new(nameof(Natural), "QrkoSkin:Natural", LocTerms.QrkoSkinNatural, CommonReferences.Get.QrkoNaturalPreview),
                Pale = new(nameof(Pale), "QrkoSkin:Pale", LocTerms.QrkoSkinPale, CommonReferences.Get.QrkoPalePreview),
                Moss = new(nameof(Moss), "QrkoSkin:Moss", LocTerms.QrkoSkinMoss, CommonReferences.Get.QrkoMossPreview),
                Moonlight = new(nameof(Moonlight), "QrkoSkin:Moonlight", LocTerms.QrkoSkinMoonlight, CommonReferences.Get.QrkoMoonlightPreview);
        }

        public class CaradocSkin : Skin {
            CaradocSkin(string enumName, string flag, string nameKey, ShareableSpriteReference preview) : base(enumName, flag, nameKey, preview) { }

            [UnityEngine.Scripting.Preserve] public static readonly CaradocSkin
                KnightErrant = new(nameof(KnightErrant), "CaradocSkin:KnightErrant", LocTerms.CaradocSkinKnightErrant, CommonReferences.Get.CaradocKnightErrantPreview),
                KnightOfTheRealm = new(nameof(KnightOfTheRealm), "CaradocSkin:KnightOfTheRealm", LocTerms.CaradocSkinKnightOfTheRealm, CommonReferences.Get.CaradocKnightOfTheRealmPreview);
        }
        
        public class ArthurSkin : Skin {
            ArthurSkin(string enumName, string flag, string nameKey, ShareableSpriteReference preview) : base(enumName, flag, nameKey, preview) { }

            [UnityEngine.Scripting.Preserve] public static readonly ArthurSkin
                TheOnceAndFutureKing = new(nameof(TheOnceAndFutureKing), "ArthurSkin:TheOnceAndFutureKing", LocTerms.ArthurSkinTheOnceAndFutureKing, CommonReferences.Get.ArthurTheOnceAndFutureKingPreview),
                ConquerorOfAvalon = new(nameof(ConquerorOfAvalon), "ArthurSkin:ConquerorOfAvalon", LocTerms.ArthurSkinConquerorOfAvalon, CommonReferences.Get.ArthurConquerorOfAvalonPreview);
        }
    }
}