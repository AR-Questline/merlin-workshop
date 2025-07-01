using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting.IngredientsView {
    [UsesPrefab("Crafting/Handcrafting/VIngredientTabContents")]
    public class VIngredientTabContents : View<IngredientTabContents>, IFocusSource {
        [SerializeField] Transform ingredientsHost;
        [SerializeField] ARButton randomIngredientsButton;
        [SerializeField] VItemsListRecipeUI recipeListUI;
        
        public bool ForceFocus => true;
        public Component DefaultFocus => randomIngredientsButton;
        public Transform IngredientsHost => ingredientsHost;

        public void Refresh() {
            recipeListUI.Refresh();
        }
    }
}