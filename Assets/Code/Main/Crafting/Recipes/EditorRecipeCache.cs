using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Recipes {
    public static class EditorRecipeCache {
        public static void ResetCache() => ConstructCache();

        static void ConstructCache() {
#if UNITY_EDITOR
            var allRecipes = TemplatesProvider.EditorGetAllOfType<BaseRecipe>(TemplateTypeFlag.Regular);
            var allItems = TemplatesProvider.EditorGetAllOfType<ItemTemplate>(TemplateTypeFlag.All);

            foreach (var item in allItems) {
                item.EditorRecipes = new();
            }

            foreach (var baseRecipe in allRecipes) {
                try {
                    var outcome = baseRecipe.Outcome;
                    outcome.EditorRecipes.Add(baseRecipe);
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
#endif
        }
    }
}