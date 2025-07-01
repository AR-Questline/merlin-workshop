using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Locations.Shops.Prices {
    public partial class CachedPriceProvider : Element, IPriceProvider {
        const int MaxCachedItems = 50;
        
        public override ushort TypeForSerialization => SavedModels.CachedPriceProvider;

        [Saved] StructList<CachedItem> _cachedItems = new(0);

        public void CleanupOldItems() {
            int count = _cachedItems.Count;
            int forceRemoveIfIndexBelow = count - MaxCachedItems;

            long now = World.Only<GameRealTime>().PlayRealTime.Ticks;
            long waitDurationTicks = TimeSpan.FromHours(4).Ticks;
            
            for (int i = count - 1; i >= 0; i--) {
                var cachedItem = _cachedItems[i];
                var item = cachedItem.Item;
                
                if (item == null) {
                    _cachedItems.RemoveAtSwapBack(i);
                } else if (i < forceRemoveIfIndexBelow || now - cachedItem.timestamp > waitDurationTicks) {
                    // It will be removed from the cache automatically by a listener in BoughtFromHeroStock
                    item.Discard();
                }
            }
        }
        
        public int SellPrice(IMerchant buyer, Item item) {
            for (int i = 0; i < _cachedItems.Count; i++) {
                if (_cachedItems[i].item.id == item.ID) {
                    return _cachedItems[i].price;
                }
            }
            throw new ArgumentException($"Item {item.DebugName} not found in CachedPriceProvider (buy back)");
        }

        public void Add(IMerchant merchant, Item item) {
            int price = BuyFromHeroPrice(merchant, item);
            _cachedItems.Add(new CachedItem {
                item = item,
                price = price,
                timestamp = World.Only<GameRealTime>().PlayRealTime.Ticks
            });
        }

        public void Remove(IModel item) {
            for (int i = 0; i < _cachedItems.Count; i++) {
                if (_cachedItems[i].item.ID == item.ID) {
                    _cachedItems.RemoveAtSwapBack(i);
                    return;
                }
            }
        }

        public Item GetItemToStackWith(IMerchant merchant, Item itemToStack) {
            if (!itemToStack.CanStack) {
                return null;
            }
            
            int price = BuyFromHeroPrice(merchant, itemToStack);
            for (int i = 0; i < _cachedItems.Count; i++) {
                var cachedItem = _cachedItems[i];
                var itemToStackWith = cachedItem.Item;
                bool canStack = itemToStackWith.Template == itemToStack.Template && cachedItem.price == price; 
                    
                if (canStack) {
                    _cachedItems[i] = new CachedItem {
                        item = cachedItem.item,
                        price = cachedItem.price,
                        timestamp = World.Only<GameRealTime>().PlayRealTime.Ticks
                    };
                    return itemToStackWith;
                }
            }
            return null;
        }
        

        int BuyFromHeroPrice(IMerchant merchant, Item item) {
            return Hero.Current.SellPriceProviderFor(item).SellPrice(merchant, item);
        }

        public partial struct CachedItem {
            public ushort TypeForSerialization => SavedTypes.CachedItem;

            [Saved] public WeakModelRef<Item> item;
            [Saved] public int price;
            [Saved] public long timestamp;

            public Item Item => item.Get();
        }
    }
}