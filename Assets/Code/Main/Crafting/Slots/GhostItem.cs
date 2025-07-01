using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.MVC.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Slots {
    [SpawnsView(typeof(VGhostItem))]
    public partial class GhostItem : CraftingItem {
        public sealed override bool IsNotSaved => true;

        public Transform DeterminedHost => ParentModel.GhostItemSlot;
        public ItemTemplate wantedItemTemplate;
        public int inventoryQuantity;

        public override int ModifiedQuantity => ParentModel.TryGetElement<InputItemQuantityUI>()?.Value ?? (ParentModel as EditableWorkbenchSlot)?.QuantityValue ?? requiredQuantity;

        public GhostItem(ItemTemplate wantedItem, int requiredQuantity, SimilarItemsData similarItem) {
            wantedItemTemplate = wantedItem;
            this.requiredQuantity = requiredQuantity;
            this.similarItem = similarItem;
            this.inventoryQuantity = similarItem.Quantity;
        }

        public void ChangeProperties(ItemTemplate wantedItem, int currentQuantity, SimilarItemsData similarItemsData) {
            wantedItemTemplate = wantedItem;
            requiredQuantity = currentQuantity;
            this.similarItem = similarItemsData;
            this.inventoryQuantity = similarItem.Quantity;
            TriggerChange();
            //Make sure text description is updated
            ParentModel.Refresh();
        }

        public override ItemTemplate WantedItemTemplate() {
            return wantedItemTemplate;
        }
    }
}
