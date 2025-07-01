using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Shops.Prices {
    public class DefaultPriceProvider : IPriceProvider {
        const int MinPriceValue = 1;
        readonly IMerchant _seller;

        public DefaultPriceProvider(IMerchant seller) {
            _seller = seller;
        }

        public int SellPrice(IMerchant buyer, Item item) {
            float priceToUse = buyer is Hero ? item.ExactBuyPrice : item.ExactPrice;
            var price = Mathf.RoundToInt(priceToUse * _seller.SellModifier * buyer.BuyModifier);
            return Mathf.Max(price, MinPriceValue);
        }
    }
}