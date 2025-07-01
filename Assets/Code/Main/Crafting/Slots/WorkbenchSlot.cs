using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Slots {
    [SpawnsView(typeof(VWorkbenchSlot))]
    public partial class WorkbenchSlot : CraftingSlot {
        public override Transform DeterminedHost => ParentModel.WorkbenchParent;
        public override Transform GhostItemSlot => View<VWorkbenchSlot>()?.GhostItemParent;
        public override Transform ItemSlot => View<VWorkbenchSlot>()?.ItemParent;

        public override void Submit() {
            ParentModel.RemoveFromWorkbenchSlot(TryGetElement<InteractableItem>());
        }

        public void TryFocusSlot() {
            var workbenchSlotButton = View<ICraftingSlotView>()?.SlotButton.button;
            World.Only<Focus>().Select(workbenchSlotButton);
        }
    }
}