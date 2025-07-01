using System;
using System.Collections.Generic;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Main.Crafting {
    public abstract class CraftingTemplate : Template {
        public abstract IEnumerable<IRecipe> Recipes { get; }


        // === Editor only
#if UNITY_EDITOR
        protected static int Comparison(TemplateReference left, TemplateReference right) {
            var leftRecipe = left.Get<IRecipe>();
            var rightRecipe = right.Get<IRecipe>();
            if (left == right) return 0;
            if (leftRecipe == null) return -1;
            if (rightRecipe == null) return 1;

            int compareTo = leftRecipe.Outcome.CompareTo(rightRecipe.Outcome);

            if (compareTo == 0) {
                compareTo = leftRecipe.Outcome.Quality.CompareTo(rightRecipe.Outcome.Quality);
            }

            if (compareTo == 0) {
                compareTo = string.Compare(
                    leftRecipe.DisplayString(),
                    rightRecipe.DisplayString(),
                    StringComparison.Ordinal);
            }

            return compareTo;
        }

        protected static string GetAssetGUID(BaseRecipe recipe) {
            string result = "";
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(recipe);
            if (!string.IsNullOrEmpty(assetPath)) {
                result = UnityEditor.AssetDatabase.AssetPathToGUID(assetPath);
            } else {
                Log.Important?.Error($"Could not find guid for: '{recipe.name.ColoredText(Color.red)}' ");
            }

            return result;
        }
#endif
    }
}