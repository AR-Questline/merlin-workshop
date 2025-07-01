using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Locations.Shops.Tabs;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors {
    public class VendorItemDescriptor : ExistingItemDescriptor {
        public VendorItemDescriptor(Item item) : base(item) {
            Price = TradeUtils.Price(Vendor.Seller, Vendor.Buyer, Item);
        }

        ShopVendorBaseUI Vendor => World.Only<ShopVendorBaseUI>();

        public override int Price { get; }
    }
}