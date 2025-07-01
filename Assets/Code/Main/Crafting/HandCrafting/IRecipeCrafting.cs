using System.Collections.Generic;
using Awaken.TG.Main.Crafting.HandCrafting.RecipeView;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting {
    public interface IRecipeCrafting : ICrafting {
        Transform GridUIParent { get; }
        Prompts Prompts { get; }
        [UnityEngine.Scripting.Preserve] bool HasItemLevelPreviewer { get; }
        
        IEnumerable<RecipeTabType> AllowedTabTypes();
        bool HasRecipesChanged(ref IEnumerable<IRecipe> recipes);
        
        public static class Events {
            public static readonly Event<IRecipeCrafting, IRecipe> OnRecipeChanged = new(nameof(OnRecipeChanged));
        }
    }
}