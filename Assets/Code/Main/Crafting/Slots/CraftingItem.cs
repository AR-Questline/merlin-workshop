using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Crafting.Slots {
    public abstract partial class CraftingItem : Element<CraftingSlot> {
        public int requiredQuantity;
        public SimilarItemsData similarItem;
        
        /// <summary>
        /// Should not be discarded. Use Drop() method instead.
        /// </summary>
        public Item Item => similarItem.Items?.FirstOrDefault(i => i is { HasBeenDiscarded: false });
        public abstract int ModifiedQuantity { get; }
        
        public abstract ItemTemplate WantedItemTemplate();

        public void Drop(int quantityMultiplier = 1) {
            ParentModel.ParentModel.SimilarItemsData.DropHeroSimilarItems(similarItem.Template, ModifiedQuantity, quantityMultiplier);
        }
    }
}
