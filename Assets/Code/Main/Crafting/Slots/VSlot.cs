using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Slots {
    public abstract class VSlot<T> : View<T>, ICraftingSlotView, ISelectableCraftingSlot where T : CraftingSlot {
        [field: SerializeField] public Transform ItemQuantityParent { [UnityEngine.Scripting.Preserve] get; private set; }
        [field: SerializeField] public Transform GhostItemParent { get; private set; }
        [field: SerializeField] public Transform ItemParent { get; private set; }
        [field: SerializeField] public ButtonConfig SlotButton { get; private set; }
        
        public override Transform DetermineHost() => Target.DeterminedHost;
        
        public void Submit() => Target.Submit();
        
        protected override void OnInitialize() {
            SlotButton.InitializeButton();
            Target.ListenTo(Model.Events.AfterChanged, Refresh, this);
            Target.ListenTo(Model.Events.AfterElementsCollectionModified, Refresh, this);
            Refresh();

            SlotButton.button.OnRelease += Target.Submit;
            SlotButton.button.OnHover += OnHover;
        }
        
        protected virtual void Refresh() {
            SlotButton.button.interactable = !Target.IsRecipeCrafting;
        }

        void OnHover(bool hover) {
            if (hover) {
                OnHoverEntered();
            } else {
                OnHoverExit();
            }
        }

        protected virtual void OnHoverEntered() { }
        protected virtual void OnHoverExit() { }
    }
}