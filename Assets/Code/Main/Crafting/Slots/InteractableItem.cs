using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Crafting.Slots {
    public partial class InteractableItem : CraftingItem {
        public override int ModifiedQuantity => requiredQuantity;
        bool Inventory { get; }
        
        public InteractableItem(SimilarItemsData similarItem, bool inventory) {
            this.similarItem = similarItem;
            this.requiredQuantity = similarItem.Quantity;
            Inventory = inventory;
        }
        
        public InteractableItem(SimilarItemsData similarItem, int requiredQuantity, bool inventory) {
            this.similarItem = similarItem;
            this.requiredQuantity = requiredQuantity;
            Inventory = inventory;
        }

        public InteractableItem TakePart(int amountToTake, bool inventory) {
            if (amountToTake > requiredQuantity) {
                amountToTake = requiredQuantity;
            }
            requiredQuantity -= amountToTake;
            if (requiredQuantity <= 0) {
                if (ParentModel.DiscardWhenEmpty) {
                    ParentModel.Discard();
                } else {
                    Discard();
                }
            } else {
                TriggerChange();
            }
            return new InteractableItem(similarItem, amountToTake, inventory);
        }
        
        public bool CanAddPart(InteractableItem interactableItem) {
            bool isValid = interactableItem != null && !interactableItem.HasBeenDiscarded &&
                           interactableItem.Item != null && !interactableItem.Item.HasBeenDiscarded &&
                           Item != null && !Item.HasBeenDiscarded;
            
            return isValid && Equals(interactableItem.Item.Template, Item.Template);
        }
        
        public void AddPart(InteractableItem interactableItem) {
            requiredQuantity += interactableItem.requiredQuantity;
            if(interactableItem.IsInitialized) {
                interactableItem.Discard();
            }
            TriggerChange();
        }

        public override ItemTemplate WantedItemTemplate() {
            return null;
        }

        protected override void OnFullyInitialized() {
            if (!Inventory) {
                World.SpawnView<VCraftingItemIconText>(this);
            }
        }
    }
}