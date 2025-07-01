using Awaken.TG.Main.Crafting.HandCrafting.IngredientsView;
using Awaken.TG.Main.Crafting.Slots;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Base;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Cooking {
    public class VCPutRandomIngredients : ViewComponent<IngredientTabContents>, ISelectableCraftingSlot {
        [SerializeField] ButtonConfig buttonConfig;
        [SerializeField] TooltipPosition tooltipPositionLeft;
        [SerializeField] TooltipPosition tooltipPositionRight;

        ExperimentalCooking ExperimentalCooking => Target.IngredientsGridUI.ParentModel;
        VSimpleTooltipSystemUI _tooltip;
        
        protected override void OnAttach() {
            _tooltip = Target.AddElement(new FloatingTooltipUI(typeof(VSimpleTooltipSystemUI), Target.ParentModel.ParentModel.View<IVCrafting>().transform, 0.1f, 0.1f)).View<VSimpleTooltipSystemUI>();
            buttonConfig.InitializeButton();
            buttonConfig.button.OnRelease += ExperimentalCooking.RandomizeIngredients;
            buttonConfig.button.OnHover += ShowTooltipOnHover;
        }

        public void Submit() => ExperimentalCooking.RandomizeIngredients();

        void ShowTooltipOnHover(bool hover) {
            if (hover) {
                _tooltip.SetPosition(tooltipPositionLeft, tooltipPositionRight);
                _tooltip.Show(LocTerms.PutRandomIngredientsIntoThePot.Translate(), LocTerms.RandomIngredientsDescription.Translate());
            } else {
                _tooltip.Hide();
            }
        }
        
        protected override void OnDiscard() {
            buttonConfig.button.OnRelease -= ExperimentalCooking.RandomizeIngredients;
            buttonConfig.button.OnHover -= ShowTooltipOnHover;
        }
    }
}