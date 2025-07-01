using System.Linq;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Slots {
    public partial class InventorySlot : CraftingSlot, IWithRecyclableView {
        public override Transform GhostItemSlot => View<VInventorySlot>()?.GhostItemParent;
        public override Transform ItemSlot => View<VInventorySlot>()?.ItemParent;
        public override Transform DeterminedHost => ParentModel.InventoryParent;
        public override bool DiscardWhenEmpty => true;
        public int Quantity => TryGetElement<InteractableItem>()?.requiredQuantity ?? 0;
        public int Index { get; private set; }

        public InteractableItem interactableItem;
        
        public InventorySlot(int index, InteractableItem interactableItemPrototype) {
            Index = index;
            interactableItem = interactableItemPrototype;
        }
        
        protected override void OnFullyInitialized() {
            World.SpawnView<VInventorySlot>(this, true, true, ParentModel.InventoryParent);
        }

        public override void Submit() {
            if (!ParentModel.WorkbenchItemsData.Any()) {
                ParentModel.PossibleResultTooltipUI.DisappearTooltip();
            }
            
            ParentModel.AddToWorkbenchSlot(TryGetElement<InteractableItem>());
        }
    }
}