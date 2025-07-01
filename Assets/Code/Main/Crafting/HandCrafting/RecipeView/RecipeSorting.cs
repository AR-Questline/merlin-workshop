using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility.Enums;
using UnityEngine.Localization.Settings;

namespace Awaken.TG.Main.Crafting.HandCrafting.RecipeView {
    public class RecipeSorting : RichEnum, IComparer<IRecipe> {

        readonly Comparer _comparer;
        readonly bool _reverse;
        readonly LocString _name;
        readonly bool _localeCheck;

        public string Name => _name;
        public bool IsAvailable => !_localeCheck || CheckLocale();

        static readonly Comparer 
            ByIngredientCount = (x, y) => Compare(x.Ingredients.Count(), y.Ingredients.Count()),
            Alphabetically = (x, y) => Compare(x.OutcomeName(), y.OutcomeName()),
            ByPrice = (x, y) => Compare(x.Outcome.BasePrice, y.Outcome.BasePrice),
            ByWeight = (x, y) => Compare(WeightOf(x), WeightOf(y));

        [UnityEngine.Scripting.Preserve]
        public static readonly RecipeSorting
            ByIngredientCountAscending = new(nameof(ByIngredientCountAscending), ByIngredientCount, true, LocTerms.RecipeComparerByIngredientCountAscending),
            ByIngredientCountDescending = new(nameof(ByIngredientCountDescending), ByIngredientCount, false, LocTerms.RecipeComparerByIngredientCountDescending),
            AlphabeticallyAscending = new(nameof(AlphabeticallyAscending), Alphabetically, true, LocTerms.RecipeComparerAlphabeticallyAscending, true),
            AlphabeticallyDescending = new(nameof(AlphabeticallyDescending), Alphabetically, false, LocTerms.RecipeComparerAlphabeticallyDescending, true),
            ByPriceAscending = new(nameof(ByPriceAscending), ByPrice, true, LocTerms.RecipeComparerByPriceAscending),
            ByPriceDescending = new(nameof(ByPriceDescending), ByPrice, false, LocTerms.RecipeComparerByPriceDescending),
            ByWeightAscending = new(nameof(ByWeightAscending), ByWeight, true, LocTerms.RecipeComparerByWeightAscending),
            ByWeightDescending = new(nameof(ByWeightDescending), ByWeight, false, LocTerms.RecipeComparerByWeightDescending);

        RecipeSorting(string enumName, Comparer comparer, bool reverse, string nameID, bool localeCheck = false) : base(enumName, "RecipeComparer") {
            _comparer = comparer;
            _reverse = reverse;
            _name = new LocString { ID = nameID };
            _localeCheck = localeCheck;
        }
        
        public int Compare(IRecipe x, IRecipe y) {
            int result = CompareInternal(x, y);
            return _reverse ? -result : result;
        }

        int CompareInternal(IRecipe x, IRecipe y) {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            int result = _comparer(x, y);
            return result == 0 ? Compare(x.Outcome.Quality, y.Outcome.Quality) : result;
        }

        static int Compare(float x, float y) => y.CompareTo(x);
        static int Compare(int x, int y) => y.CompareTo(x);
        static int Compare(string x, string y) => string.Compare(y, x, StringComparison.Ordinal);

        static float WeightOf(IRecipe recipe) => recipe.Outcome.Weight;

        static bool CheckLocale() => !CommonReferences.Get.NonAlphabetLanguages.Contains(LocalizationSettings.SelectedLocale);

        delegate int Comparer(IRecipe x, IRecipe y);
    }
}