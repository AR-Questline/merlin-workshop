using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Locations.Shops.Tabs {
    public partial class ShopSellUI : ShopVendorBaseUI {
        protected override string TradeActionName => LocTerms.Sell.Translate();
        public override IMerchant Seller => Hero;
        public override IMerchant Buyer => Shop;
        public override IEnumerable<Item> Items => Hero.HeroItems.SellableInventory(Shop.AdditionalSellCondition);

        protected override void OnSuccessfulTrade() {
            View.PlaySellSfx();
        }
        
        protected override void SetupEmptyInfoLabels() {
            View<IEmptyInfo>().EmptyInfoView.SetupLabels(LocTerms.EmptyShopSellInfo.Translate(), LocTerms.EmptyShopSellDesc.Translate());
        }
    }
}