using Awaken.TG.Main.Crafting.Recipes;

namespace Awaken.TG.Main.UI.HUD.AdvancedNotifications.MiddleScreen.Recipe {
    public readonly struct RecipeData {
        public readonly IRecipe recipe;
        
        public RecipeData(IRecipe recipe) {
            this.recipe = recipe;
        }
    }
}