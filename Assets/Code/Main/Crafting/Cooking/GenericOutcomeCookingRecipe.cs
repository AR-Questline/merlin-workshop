using System;
using System.Collections.Generic;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;
using Awaken.Utility.Sessions;

namespace Awaken.TG.Main.Crafting.Cooking {
    public class GenericOutcomeCookingRecipe : IRuntimeRecipe {
        public bool IsHidden => true;
        public Ingredient[] Ingredients => Array.Empty<Ingredient>();
        public ItemTemplate Outcome { get; }
        public int Quantity => 1;
        public bool CanHaveItemLevel => false;
        public float ItemCraftingDifficulty => 0;
        public RecipeStatRequirement StatRequirement => new(null, 0);
        
        string INamed.DisplayName => OutcomeName();
        string INamed.DebugName => nameof(GenericOutcomeCookingRecipe) + " " + Outcome?.DebugName;

        public ProfStatType ProficiencyStat => ProfStatType.Cooking;
        public HeroStatType BonusLevelStat => HeroStatType.CookingLevelBonus;

        Cached<GenericOutcomeCookingRecipe, string> _translation = new(static recipe => recipe.Outcome.ItemName);

        public GenericOutcomeCookingRecipe(ItemTemplate outcome) {
            Outcome = outcome;
        }
        
        public bool ExperimentalMatch(IEnumerable<ItemData> items) {
            return true;
        }

        public bool RecipeMatch(IEnumerable<SimilarItemsData> items) {
            return true;
        }

        public Item Create(ICrafting crafting, int? overridenLevel = null) {
            return new Item(Outcome, 1, 0);
        }

        public string DisplayString() {
            return $"Any => {Outcome?.ItemName}";
        }

        public string OutcomeName() {
            return _translation.Get(this);
        }
        
        public void StartStoryOnCreation() { }
    }
}