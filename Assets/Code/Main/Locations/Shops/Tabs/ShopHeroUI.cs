using Awaken.TG.Main.Localization;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Locations.Shops.Tabs {
    public abstract class ShopHeroUI : ShopVendorBaseUI {
        protected override string TradeActionName => LocTerms.Sell.Translate();
        public override IMerchant Seller => Hero;
        public override IMerchant Buyer => Shop;
        
        protected override void OnSuccessfulTrade() {
            View.PlaySellSfx();
        }
    }
}