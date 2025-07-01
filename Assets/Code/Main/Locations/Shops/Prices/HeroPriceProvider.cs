using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Shops.Prices {
    public class HeroPriceProvider : IPriceProvider {
        const int MinPriceValue = 1;
        readonly IMerchant _seller;

        public HeroPriceProvider(Hero hero) {
            _seller = hero;
        }

        public int SellPrice(IMerchant buyer, Item item) {
            float stolenModifier = GetStolenModifier(buyer, item);

            float sellingToBuyerPrice = item.ExactPrice * stolenModifier * _seller.SellModifier * buyer.BuyModifier;
            float buyingFromBuyerPrice = item.ExactBuyPrice * stolenModifier * buyer.SellModifier * _seller.BuyModifier;

            int finalPrice = Mathf.RoundToInt(Mathf.Min(sellingToBuyerPrice, buyingFromBuyerPrice));
            return Mathf.Max(finalPrice, MinPriceValue);
        }

        float GetStolenModifier(IMerchant buyer, Item item) {
            if (!item.IsStolen || buyer is not Shop shop) {
                return 1.0f;
            }
            
            if (!shop.Template.IsFence) {
                return 0.0f;
            }
            
            float stolenModifier = shop.Template.fenceBuyModifierMultiplier;
            if (_seller is Hero hero) {
                stolenModifier += hero.Stat(HeroStatType.FenceSellBonusMultiplier);
            }
            return Mathf.Min(stolenModifier, 1.0f);
        }
    }
}