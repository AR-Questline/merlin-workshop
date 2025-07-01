using System.Collections.Generic;
using Awaken.TG.Main.Crafting.Slots;

namespace Awaken.TG.Main.Heroes.Items {
    public struct ItemData {
        public Item item;
        public int quantity;
        
        public ItemData(Item item, int quantity) {
            this.item = item;
            this.quantity = quantity;
        }

        public static implicit operator ItemData(CraftingItem craftingItem) => new(craftingItem?.Item, craftingItem?.requiredQuantity ?? 0);
        public static explicit operator ItemData(Item item) => new(item, item?.Quantity ?? 0);
    }

    public struct SimilarItemsData {
        public ItemTemplate Template { get; }
        public List<Item> Items { get; }
        public int Quantity { get; private set; }

        public SimilarItemsData(ItemData itemData) {
            Template = itemData.item.Template;
            Items = new List<Item> { itemData.item };
            Quantity = itemData.quantity;
        }

        public static SimilarItemsData operator +(SimilarItemsData a, ItemData b) {
            if (a.Template == b.item.Template) {
                a.Items.Add(b.item);
                a.Quantity += b.quantity;
            }

            return a;
        }
    }
}