using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Shops.Tabs {
    public class VCShopUITabButton : ShopUITabs.VCHeaderTabButton {
        [RichEnumExtends(typeof(ShopUITabType))] 
        [SerializeField] RichEnumReference tabType;

        public override ShopUITabType Type => tabType.EnumAs<ShopUITabType>();
        public override string ButtonName => Type.Title;
    }
}