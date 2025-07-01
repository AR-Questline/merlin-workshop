using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Shops.UI;
using Awaken.TG.Main.Utility.RichEnums;
using UnityEngine;

namespace Awaken.TG.Main.UI.Components {
    public class VCShopCurrency : StatComponent<ShopUI> {
        [Space(10F)]
        [RichEnumExtends(typeof(CurrencyStatType))]
        public RichEnumReference statType;

        protected override IWithStats WithStats => Target.Shop;
        protected override StatType StatType => statType.EnumAs<CurrencyStatType>();
    }
}