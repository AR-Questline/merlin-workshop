using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Locations.Shops.Prices;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Relations;

namespace Awaken.TG.Main.Locations.Shops.Stocks {
    public abstract partial class Stock : Element<Shop> {
        public virtual IPriceProvider PriceProvider => new DefaultPriceProvider(ParentModel);

        public virtual bool RestockOnce { get; }
        public virtual void Restock() { }
        
        public Item AddItem(Item item, bool allowStacking = true) {
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

        protected virtual Item StacksWith(Item item) {
            return item.CanStack ? Items.FirstOrDefault(i => i.Template == item.Template) : null;
        }

        public void RemoveItem(Item item, bool discard) {
            if (discard) {
                item.Discard();
            } else {
                RelatedList(Relations.Stocks).Remove(item);
                ParentModel.RelatedList(IItemOwner.Relations.Owns).Remove(item);
            }
        }

        public IEnumerable<Item> Items => RelatedList(Relations.Stocks);
        
        public static class Relations {
            static readonly RelationPair<Stock, Item> Stocking = new(typeof(Relations), Arity.One, nameof(Stocks), Arity.Many, nameof(StockedBy));
            public static readonly Relation<Stock, Item> Stocks = Stocking.LeftToRight;
            public static readonly Relation<Item, Stock> StockedBy = Stocking.RightToLeft;
        }
    }
}