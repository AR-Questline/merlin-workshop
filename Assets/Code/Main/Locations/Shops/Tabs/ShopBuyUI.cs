using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Locations.Shops.Tabs {
    public partial class ShopBuyUI : ShopVendorBaseUI {
        protected override string TradeActionName => LocTerms.Buy.Translate();
        public override IMerchant Seller => Shop;
        public override IMerchant Buyer => Hero;
        public override IEnumerable<Item> Items => Shop.Items.Except(Shop.BoughtFromHeroStock.Items);

        protected override void SetupEmptyInfoLabels() {
            View<IEmptyInfo>().EmptyInfoView.SetupLabels(LocTerms.EmptyShopBuyInfo.Translate(), LocTerms.EmptyShopBuyDesc.Translate());
        }
    }

    public partial class ShopBuyBackUI : ShopBuyUI {
        public override IEnumerable<Item> Items => Shop.BoughtFromHeroStock.Items;
        
        protected override void SetupEmptyInfoLabels() {
            View<IEmptyInfo>().EmptyInfoView.SetupLabels(LocTerms.EmptyNoItems.Translate(), LocTerms.EmptyShopBuyBackDesc.Translate());
        }
    }
}