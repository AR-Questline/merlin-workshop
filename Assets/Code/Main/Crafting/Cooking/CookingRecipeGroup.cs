using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Cooking {
    [Serializable]
    public class CookingRecipeGroup {
        [SerializeField, ReadOnly, TemplateType(typeof(ItemTemplate))] TemplateReference outcome;
        [SerializeField] 
        [TemplateType(typeof(CookingRecipe))]
        List<TemplateReference> recipes;

        public ItemTemplate Outcome => outcome?.Get<ItemTemplate>();
        public List<TemplateReference> Recipes => recipes;

        public CookingRecipeGroup(ItemTemplate outcome) {
            this.outcome = new TemplateReference(outcome);
            recipes = new List<TemplateReference>();
        }

        public void AddRecipe(TemplateReference recipe) {
            if (recipes.Contains(recipe)) {
                return;
            }
            recipes.Add(recipe);
        }

        public void DedupeRecipes() {
            recipes = recipes.Distinct().ToList();
        }
    }
}