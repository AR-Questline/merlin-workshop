using Awaken.TG.Main.Crafting.HandCrafting;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using TMPro;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Slots {
    [UsesPrefab("Crafting/Handcrafting/" + nameof(VEditableWorkbenchSlot))]
    public class VEditableWorkbenchSlot : VWorkbenchSlot<EditableWorkbenchSlot> {
        const float ButtonHoldDuration = 0.4f;
        
        [SerializeField] TMP_Text quantityText;
        [SerializeField] GameObject quantityParent;
        [SerializeField] ButtonConfig buttonIncrease;
        [SerializeField] ButtonConfig buttonDecrease;
        
        protected override void OnFullyInitialized() {
            quantityParent.SetActiveOptimized(false);
            quantityText.SetText(string.Empty);

            buttonIncrease.InitializeButton();
            buttonDecrease.InitializeButton();
            SlotButton.button.OnEvent += OnButtonEvent;
            
            buttonIncrease.button.OnHold += hold => {
                if (hold > ButtonHoldDuration) {
                    Target.IncreaseValue();
                }
            };
            buttonDecrease.button.OnHold += hold => {
                if (hold > ButtonHoldDuration) {
                    Target.DecreaseValue();
                }
            };
            
            buttonIncrease.button.OnRelease += Target.IncreaseValue;
            buttonDecrease.button.OnRelease += Target.DecreaseValue;
            buttonIncrease.button.Interactable = false;
            buttonDecrease.button.Interactable = false;
            buttonIncrease.TrySetActiveOptimized(false);
            buttonDecrease.TrySetActiveOptimized(false);

            Target.ListenTo(EditableWorkbenchSlot.Events.OnIngredientQuantityChanged, q => OnIngredientQuantityChanged(q.newQuantity), this);
        }
        
        UIResult OnButtonEvent(UIEvent evt) {
            if (RewiredHelper.IsGamepad && evt is UIAction action && action.Name == KeyBindings.UI.Generic.Cancel) {
                World.All<RecipeSlot>().First(rs => rs.IsSelected).View<VRecipeSlot>().SelectSlot();
                return UIResult.Accept;
            }
            
            return Target.OnButtonEvent(evt);
        }
        
        void OnIngredientQuantityChanged(int newQuantity) {
            buttonIncrease.TrySetActiveOptimized(true);
            buttonDecrease.TrySetActiveOptimized(true);
            quantityParent.SetActiveOptimized(true);
            
            var item = Target.Element<GhostItem>();
            bool canCraft = item.inventoryQuantity >= item.requiredQuantity;

            quantityText.SetText($"{item.inventoryQuantity.ToString().ColoredText(canCraft ? ARColor.MainGrey : ARColor.MainRed)}/{newQuantity}");
            
            buttonIncrease.button.Interactable = newQuantity < Target.UpperBound && Target.CanChangeQuantity;
            buttonDecrease.button.Interactable = newQuantity > Target.LowerBound && Target.CanChangeQuantity;
            Target.ParentModel.TriggerChange();
        }

        protected override void OnHoverEntered() {
            base.OnHoverEntered();
            Target.SetupModificationIngredientsState(true);
        }

        protected override void OnHoverExit() {
            base.OnHoverExit();
            Target.SetupModificationIngredientsState(false);
        }
    }
}