using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Locations.Shops {
    public static class TradeUtils {
        public static bool TryTrade(IMerchant seller, IMerchant buyer, Item item, int itemQuantity) {
            int price = Price(seller, buyer, item, itemQuantity);
            var context = new ContractContext(seller, buyer, ChangeReason.Trade);
            if (buyer.Wealth >= price) {
                item.MoveTo(buyer.Inventory, itemQuantity);
                
                seller.Wealth.IncreaseBy(price, context);
                seller.Trigger(IMerchant.Events.ItemSold, item);
                
                buyer.Wealth.DecreaseBy(price, context);
                buyer.Trigger(IMerchant.Events.ItemBought, item);
                
                return true;
            }
            return false;
        }

        public static int AffordableItemsAmount(IMerchant seller, IMerchant buyer, Item item) {
            float amount = buyer.Wealth / Price(seller, buyer, item);
            return amount > int.MaxValue ? int.MaxValue : (int)amount;
        }

        public static int Price(IMerchant seller, IMerchant buyer, Item item, int count = 1) {
            return seller.SellPriceProviderFor(item).SellPrice(buyer, item) * count;
        }
    }
}