using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Slots {
    [SpawnsView(typeof(VEditableWorkbenchSlot))]
    public partial class EditableWorkbenchSlot : WorkbenchSlot {
        const int IncreaseStep = 1;
        const int DecreaseStep = -1;

        public override Transform GhostItemSlot => CurrentView.GhostItemParent;
        public override Transform ItemSlot => CurrentView.ItemParent;
        public int UpperBound { get; private set; }
        public int LowerBound { get; private set; }
        public int QuantityValue { get; private set; }
        public bool CanChangeQuantity { get; private set; }
        
        VEditableWorkbenchSlot CurrentView => View<VEditableWorkbenchSlot>();
        bool _canModifyIngredients;
        
        public new static class Events {
            public static readonly Event<WorkbenchSlot, (Item ingredient, int newQuantity)> OnIngredientQuantityChanged = new(nameof(OnIngredientQuantityChanged));
        }
        
        public override void UpdateSlot(Ingredient ingredient, int itemQuantity, bool canChangeQuantity) {
            LowerBound = ingredient?.Count ?? 0;
            UpperBound = itemQuantity;
            CanChangeQuantity = canChangeQuantity;
            
            QuantityValue = LowerBound;
            TryTriggerQuantityChangedEvent();
        }
        
        public void SetupModificationIngredientsState(bool hover) {
            if (CraftingItem == null) {
                _canModifyIngredients = false;
                return;
            }
            
            _canModifyIngredients = hover && CanChangeQuantity;
        }

        public void IncreaseValue() => ChangeValue(IncreaseStep);
        public void DecreaseValue() => ChangeValue(DecreaseStep);

        void ChangeValue(int step) {
            var newValue = math.clamp(QuantityValue + step, LowerBound, UpperBound);
            
            if (newValue == QuantityValue) {
                return;
            }
            
            QuantityValue = newValue;
            TryTriggerQuantityChangedEvent();
        }
        
        void TryTriggerQuantityChangedEvent() {
            if (CraftingItem != null) {
                this.Trigger(Events.OnIngredientQuantityChanged, (CraftingItem.Item, QuantityValue));
            }
        }
        
        public UIResult OnButtonEvent(UIEvent evt) {
            if (!_canModifyIngredients) {
                return UIResult.Ignore;
            }

            switch (evt) {
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.IncreaseValueAlt:
                case UIKeyLongHeldAction holdAction when holdAction.Name == KeyBindings.UI.Generic.IncreaseValueAlt:
                    IncreaseValue();
                    return UIResult.Accept;
                case UIKeyDownAction action when action.Name == KeyBindings.UI.Generic.DecreaseValueAlt:
                case UIKeyLongHeldAction holdAction when holdAction.Name == KeyBindings.UI.Generic.DecreaseValueAlt:
                    DecreaseValue();
                    return UIResult.Accept;
                default:
                    return UIResult.Ignore;
            }
        }
    }
}