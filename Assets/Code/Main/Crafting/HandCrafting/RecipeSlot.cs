using Awaken.TG.Main.Crafting.HandCrafting.RecipeView;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;

namespace Awaken.TG.Main.Crafting.HandCrafting {
    public partial class RecipeSlot : Element<RecipeTabContents>, IWithRecyclableView {
        public sealed override bool IsNotSaved => true;

        public IRecipe Recipe { get; protected set; }
        public bool IsSelected { get; private set; }
        public int Index { get; private set; }
        public bool IsCraftable => CraftingUtils.IsRecipeCraftable(Recipe, ParentModel.ParentModel.ParentModel);

        public RecipeSlot(IRecipe recipe, int index) {
            Recipe = recipe;
            Index = index;
        }

        protected override void OnFullyInitialized() {
            World.SpawnView<VRecipeSlot>(this, true, true, ParentModel.RecipeHost);
        }
        
        public void SelectSlot() {
            if (IsSelected) return;

            IsSelected = true;
            if (View<VRecipeSlot>() is { } view) {
                ParentModel.ClickRecipe(this);
                view.SelectSlot();
            }
        }

        public void UnselectSlot() {
            if (!IsSelected) return;

            IsSelected = false;
            if (View<VRecipeSlot>() is { } view) {
                view.UnselectSlot();
            }
        }
        
        public void RefreshIndex(int index) {
            Index = index;
            TriggerChange();
        }
    }
}