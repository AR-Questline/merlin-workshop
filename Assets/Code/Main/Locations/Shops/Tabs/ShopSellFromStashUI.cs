using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Storage;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Locations.Shops.Tabs {
    public partial class ShopSellFromStashUI : ShopSellUI {
        public override IEnumerable<Item> Items => Hero.Element<HeroStorage>().SellableInventory(Shop.AdditionalSellCondition);
        
        protected override void SetupEmptyInfoLabels() {
            View<IEmptyInfo>().EmptyInfoView.SetupLabels(LocTerms.EmptyShopSellInfo.Translate(), LocTerms.EmptyShopSellFromStashDesc.Translate());
        }
    }
}