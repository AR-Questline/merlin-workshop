using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Slots {
    [UsesPrefab("Crafting/Handcrafting/VWorkbenchSlot")]
    public class VWorkbenchSlot : VWorkbenchSlot<WorkbenchSlot> { }

    public class VWorkbenchSlot<T> : VSlot<T> where T : CraftingSlot {
        [SerializeField] protected ItemSelectionComponent selection;

        Item _tmpItem;
        
        void ClearHoverTooltip() {
            DiscardTmpItem();
            selection.ForceUnhover();
        }
        
        void DiscardTmpItem() {
            _tmpItem?.Discard();
            _tmpItem = null;
        }
        
        protected override void OnHoverEntered() {
            if (Target.Item) {
                selection.ForceHover(Target.Item);
            } else if (Target.WantedItemTemplate != null) {
                _tmpItem?.Discard(); 
                _tmpItem = World.Add(new Item(Target.WantedItemTemplate));
                _tmpItem.MarkedNotSaved = true;
                selection.ForceHover(_tmpItem);
            }
        }

        protected override void OnHoverExit() {
            ClearHoverTooltip();
        }

        protected override IBackgroundTask OnDiscard() {
            DiscardTmpItem();
            return base.OnDiscard();
        }

        protected override void Refresh() {
            base.Refresh();
            ClearHoverTooltip();
        }
    }
}