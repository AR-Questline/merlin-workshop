using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Storage {
    public class VCHeroStorageTabButton : HeroStorageTabs.VCHeaderTabButton {
        [RichEnumExtends(typeof(HeroStorageTabType))] 
        [SerializeField] RichEnumReference tabType;

        public override HeroStorageTabType Type => tabType.EnumAs<HeroStorageTabType>();
        public override string ButtonName => Type.Title;
    }
}