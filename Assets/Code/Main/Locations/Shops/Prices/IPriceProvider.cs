using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.Main.Locations.Shops.Prices {
    public interface IPriceProvider {
        int SellPrice(IMerchant buyer, Item item);
    }
}