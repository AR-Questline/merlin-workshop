using Awaken.Utility;
using System.Collections.Generic;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;

namespace Awaken.TG.Main.Heroes {
    public partial class HeroRecipes : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroRecipes;

        [Saved] public HashSet<IRecipe> knownRecipes = new();

        public void LearnRecipe(IRecipe recipe) {
            if (knownRecipes.Add(recipe)) {
                ItemUtils.AnnounceGettingRecipe(recipe, ParentModel);
                this.Trigger(Events.RecipeLearned, recipe);
            }
        }

        public void ForgetRecipe(IRecipe recipe) {
            knownRecipes.Remove(recipe);
        }

        public bool IsLearned(IRecipe recipe) {
            return knownRecipes.Contains(recipe);
        }

        public new static class Events {
            public static readonly Event<HeroRecipes, IRecipe> RecipeLearned = new(nameof(RecipeLearned));
        }
    }
}