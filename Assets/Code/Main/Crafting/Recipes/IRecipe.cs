using System.Collections.Generic;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Crafting.Recipes {
    public interface IRecipe : ITemplate {
        bool CanHaveItemLevel { get; }
        float ItemCraftingDifficulty { get; }
        RecipeStatRequirement StatRequirement { get; }
        bool IsHidden { get; }
        Ingredient[] Ingredients { get; }
        ItemTemplate Outcome { get; }
        int Quantity { get; }
        ProfStatType ProficiencyStat { get; }
        HeroStatType BonusLevelStat { get; }
        
        bool ExperimentalMatch(IEnumerable<ItemData> items);
        bool RecipeMatch(IEnumerable<SimilarItemsData> items);
        Item Create(ICrafting crafting = null, int? overridenLevel = null);
        
        string DisplayString();
        string OutcomeName();
        void StartStoryOnCreation();
    }
}