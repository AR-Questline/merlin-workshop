using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Locations.Shops.Prices;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Shops.Stocks {
    public abstract partial class Stock : Element<Shop> {
        public virtual IPriceProvider PriceProvider => new DefaultPriceProvider(ParentModel);

        protected bool _isCompressed;
        
        public virtual bool RestockOnce { get; }
        public virtual void Restock() { }
        protected abstract List<ItemSpawningDataRuntime> CompressedItems { get; }
        
        protected override void OnFullyInitialized() {
            CompressAllItemsWithDelay().Forget();
        }
        
        public Item AddItem(Item item, bool allowStacking = true) {
            if (_isCompressed) {
                throw new InvalidOperationException("Cannot add items to a compressed stock. Please uncompress the stock first.");
            }
            if (allowStacking) {
                var existingItem = StacksWith(item);
                if (existingItem != null) {
                    existingItem.ChangeQuantity(item.Quantity);
                    if (item.IsInitialized) {
                        item.Discard();
                    }

                    return existingItem;
                }
            }

            if (!item.IsInitialized) {
                World.Add(item);
            }
            item.MoveToDomain(CurrentDomain);
            
            ParentModel.RelatedList(IItemOwner.Relations.Owns).Add(item);
            RelatedList(Relations.Stocks).Add(item);
            return item;
        }
        
        protected ItemSpawningDataRuntime AddItemData(ItemSpawningDataRuntime itemData, bool allowStacking = true) {
            if (allowStacking) {
                var existingItem = StacksWith(itemData);
                if (existingItem != null) {
                    existingItem.quantity += itemData.quantity;
                    return existingItem;
                }
            }

            CompressedItems.Add(itemData);
            return itemData;
        }

        /// <summary>
        /// Can be performed only if the shop is open (or items are uncompressed manually)
        /// </summary>
        protected virtual Item StacksWith(Item item) {
            if (_isCompressed) {
                throw new InvalidOperationException("Cannot check stacking on a compressed stock. Please uncompress the stock first.");
            }
            return item.CanStack ? Items.FirstOrDefault(i => i.Template == item.Template) : null;
        }
        
        protected virtual ItemSpawningDataRuntime StacksWith(ItemSpawningDataRuntime item) {
            return item.ItemTemplate.canStack ? CompressedItems.FirstOrDefault(i => i.ItemTemplate == item.ItemTemplate) : null;
        }

        /// <summary>
        /// Can be performed only if the shop is open (or items are uncompressed manually)
        /// </summary>
        public void RemoveItem(Item item, bool discard) {
            if (_isCompressed) {
                throw new InvalidOperationException("Cannot remove items from a compressed stock. Please uncompress the stock first.");
            }
            if (discard) {
                item.Discard();
            } else {
                RelatedList(Relations.Stocks).Remove(item);
                ParentModel.RelatedList(IItemOwner.Relations.Owns).Remove(item);
            }
        }

        public void RemoveAllItemsOfTemplate(ItemTemplate template) {
            if (_isCompressed) {
                RemoveCompressedItemsOfTemplate(template);
            } else {
                foreach (var item in Items) {
                    if (item.Template == template) {
                        item.Discard();
                    }
                }
            }
        }

        protected virtual void RemoveCompressedItemsOfTemplate(ItemTemplate template) {
            for (int i = CompressedItems.Count - 1; i >= 0; i--) {
                if (CompressedItems[i].ItemTemplate == template) {
                    CompressedItems.RemoveAt(i);
                }
            }
        }

        protected RelatedList<Item> RelatedItems => RelatedList(Relations.Stocks);
        
        /// <summary>
        /// Can be performed only if the shop is open (or items are uncompressed manually)
        /// </summary>
        public RelatedList<Item> Items {
            get {
                if (_isCompressed) {
                    throw new InvalidOperationException("Cannot get items from a compressed stock. Please uncompress the stock first.");
                }
                return RelatedItems;
            }
        }
        
        public void ShopOpened() {
            CreateAllItems();
        }

        public void ShopClosed() {
            CompressAllItems();
        }

        async UniTaskVoid CompressAllItemsWithDelay() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            CompressAllItems();
        }
        
        protected virtual void CompressAllItems() {
            for (int i = 0; i < RelatedItems.Count; i++) {
                CompressedItems.Add(new ItemSpawningDataRuntime(RelatedItems[i]));
            }
            for (int i = RelatedItems.Count - 1; i >= 0; i--) {
                RelatedItems[i].Discard();
            }
            StatTweak.CleanupObsoleteStatTweaks();
            _isCompressed = true;
        }

        protected virtual void CreateAllItems() {
            _isCompressed = false;
            foreach (var spawningDataRuntime in CompressedItems) {
                if (spawningDataRuntime == null) continue;
                var item = World.Add(new Item(spawningDataRuntime));
                RegisterCreatedItem(item);
            }
            CompressedItems.Clear();
        }

        protected void RegisterCreatedItem(Item item) {
            item.MoveToDomain(CurrentDomain);
            ParentModel.RelatedList(IItemOwner.Relations.Owns).Add(item);
            Items.Add(item);
        }

        public void OnDeath() {
            if (_isCompressed) {
                return;
            }
            foreach (var item in Items.ToList()) {
                item.Discard();
            }
        }
        
        public static class Relations {
            static readonly RelationPair<Stock, Item> Stocking = new(typeof(Relations), Arity.One, nameof(Stocks), Arity.Many, nameof(StockedBy));
            public static readonly Relation<Stock, Item> Stocks = Stocking.LeftToRight;
            public static readonly Relation<Item, Stock> StockedBy = Stocking.RightToLeft;
        }
    }
}