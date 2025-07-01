using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Shops.Prices;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Locations.Shops {
    public interface IMerchant : IWithStats, IItemOwner {
        IPriceProvider SellPriceProviderFor(Item item);

        public MerchantStats MerchantStats { get; }
        
        /// <summary>
        /// It modifies the price when this merchant buys item
        /// </summary>
        public Stat BuyModifier => MerchantStats.BuyModifier;
        
        /// <summary>
        /// It modifies the price when this merchant sells item
        /// </summary>
        public Stat SellModifier => MerchantStats.SellModifier;
        
        public CurrencyStat Wealth => MerchantStats.Wealth;
        public CurrencyStat Cobweb => MerchantStats.Cobweb;

        public new static class Events {
            public static readonly Event<IMerchant, Item> ItemSold = new(nameof(ItemSold));
            public static readonly Event<IMerchant, Item> ItemBought = new(nameof(ItemBought));
        }
    }
}