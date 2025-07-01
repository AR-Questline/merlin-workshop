using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility.Enums;
using UnityEngine.Localization.Settings;

namespace Awaken.TG.Main.Crafting.HandCrafting.IngredientsView {
    public class IngredientsSorting : RichEnum, IComparer<Item> {
        readonly Comparer _comparer;
        readonly bool _reverse;
        readonly LocString _name;
        readonly bool _localeCheck;
        
        public string Name => _name;
        public bool IsAvailable => !_localeCheck || CheckLocale();

        [UnityEngine.Scripting.Preserve]
        static readonly Comparer
            ByNewest = (_, _) => 0,
            Alphabetically = (x, y) => Compare(x.DisplayName, y.DisplayName),
            ByLevel = (x, y) => Compare(x.Level, y.Level),
            ByQuality = (x, y) => Compare(x.Quality, y.Quality),
            ByPrice = (x, y) => Compare(x.Price, y.Price);
        
        public static readonly IngredientsSorting
            AlphabeticallyAscending = new(nameof(AlphabeticallyAscending), Alphabetically, true, LocTerms.RecipeComparerAlphabeticallyAscending, true),
            AlphabeticallyDescending = new(nameof(AlphabeticallyDescending), Alphabetically, false, LocTerms.RecipeComparerAlphabeticallyDescending, true),
            ByPriceAscending = new(nameof(ByPriceAscending), ByPrice, true, LocTerms.RecipeComparerByPriceAscending),
            ByPriceDescending = new(nameof(ByPriceDescending), ByPrice, false, LocTerms.RecipeComparerByPriceDescending);

        IngredientsSorting(string enumName, Comparer comparer, bool reverse, string nameID, bool localeCheck = false) : base(enumName, "IngredientComparer") {
            _comparer = comparer;
            _reverse = reverse;
            _name = new LocString { ID = nameID };
            _localeCheck = localeCheck;
        }
        
        public int Compare(Item x, Item y) {
            int result = CompareInternal(x, y);
            return _reverse ? -result : result;
        }
        
        static int Compare(float x, float y) => y.CompareTo(x);
        static int Compare(int x, int y) => y.CompareTo(x);
        static int Compare(string x, string y) => string.Compare(y, x, StringComparison.Ordinal);
        static int Compare(Stat x, Stat y) => Compare(x.ModifiedValue, y.ModifiedValue);
        
        int CompareInternal(Item x, Item y) {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            int result = _comparer(x, y);
            return result == 0 ? Compare(x.Level, y.Level) : result;
        }
        
        static bool CheckLocale() => !CommonReferences.Get.NonAlphabetLanguages.Contains(LocalizationSettings.SelectedLocale);

        delegate int Comparer(Item x, Item y);
    }
}