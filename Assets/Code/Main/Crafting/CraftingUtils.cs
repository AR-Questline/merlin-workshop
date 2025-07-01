using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Crafting.Slots;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Main.Crafting {
    public static class CraftingUtils {
        static readonly List<ItemTemplate> ProvidedItemTemplates = new(8);
        static readonly List<Item> UnusedItems = new(8);
        static readonly List<int> ItemQuantity = new(8);

        public static bool DistinctItemsMatchIngredientsWithQuantity(Ingredient[] requiredIngredients, IEnumerable<SimilarItemsData> provided) {
            SortIngredients(requiredIngredients);
            ItemQuantity.Clear();
            ProvidedItemTemplates.Clear();

            foreach (var similarItemsData in provided.WhereNotNull()) {
                ProvidedItemTemplates.Add(similarItemsData.Template);
                ItemQuantity.Add(similarItemsData.Quantity);
            }
            
            int foundItems = 0;

            foreach (var required in requiredIngredients) {
                for (int i = 0; i < ProvidedItemTemplates.Count; i++) {
                    //Is the ingredient the same as the provided item, and is the quantity enough
                    if (required.Count <= ItemQuantity[i] && required.Match(ProvidedItemTemplates[i])) {
                        ItemQuantity[i] -= required.Count;
                        foundItems++;
                        break;
                    }
                }
            }

            return foundItems == requiredIngredients.Length;
        }
        
        /// <summary>
        /// Allows same item in multiple slots. Each slot counts as one item (Cooking)
        /// </summary>
        public static bool MultipleItemsMatchIngredients(Ingredient[] requiredIngredients, IEnumerable<ItemData> provided) {
            return CheckIngredients(requiredIngredients, provided) is { missing: 0, unused: 0 };
        }
        
        public static IngredientCheckResult CheckIngredients(IReadOnlyList<Ingredient> requiredIngredients, IEnumerable<ItemData> provided) {
            UnusedItems.Clear();
            ItemQuantity.Clear();
            ItemQuantity.AddRange(requiredIngredients.Select(x => x.Count));
            
            foreach (var workbenchItem in provided.WhereNotNull()) {
                Item item = workbenchItem.item;
                UnusedItems.Add(item);
                int ownedItemCount = workbenchItem.quantity;
                for (int i = 0; i < requiredIngredients.Count; i++) {
                    if (ItemQuantity[i] > 0 && requiredIngredients[i].Match(item)) {
                        int toTake = Mathf.Min(ItemQuantity[i], ownedItemCount);
                        ItemQuantity[i] -= toTake;
                        ownedItemCount -= toTake;
                        if (ownedItemCount <= 0 || ItemQuantity[i] == 0) {
                            UnusedItems.Remove(item);
                            break;
                        }
                    }
                }
            }
            
            return new(ItemQuantity.Sum(), UnusedItems);
        }

        public static void SortIngredients(Ingredient[] ingredients) {
            Array.Sort(ingredients, (i1, i2) => CompareTemplatesToMatch(i1.Template, i2.Template));
        }

        public static float GetItemLevelForCrafted(IRecipe recipe) {
            GameConstants constants = World.Services.Get<GameConstants>();
            Stat proficiency = Hero.Current.Stat(recipe.ProficiencyStat);
            // Practicality should be added to bonusLevel from GameConstants -> Rpg Hero Stats.
            Stat bonusLevel = Hero.Current.Stat(recipe.BonusLevelStat);
            
            float itemLvlFromProficiency = (proficiency - 10) * constants.craftedItemLevelProficiencyMultiplier;
            float bonusItemLevel = bonusLevel + Hero.Current.HeroStats.CraftingSkillBonus;
            float difficulty = recipe.ItemCraftingDifficulty;
            return itemLvlFromProficiency + bonusItemLevel - difficulty;
        }

        public static float CalculateBonusLevelFromIngredients(IRecipe recipe, IEnumerable<CraftingItem> currentIngredients) {
            if (currentIngredients == null) {
                return 0f;
            }
            
            float craftingFactor = GameConstants.Get.craftingFactor;
            float ingredientsCalculation = 0f;
            
            foreach (CraftingItem ingredient in currentIngredients) {
                Ingredient recipeIngredient = recipe.Ingredients.FirstOrDefault(recipeIngredient => IngredientMatch(recipeIngredient.Template, ingredient.similarItem.Template));
                if (recipeIngredient == null) {
                    // Invalid state, ingredients don't match with workbench items
                    return int.MinValue;
                }
                
                int additionalAmount = Mathf.Max(0, ingredient.ModifiedQuantity - recipeIngredient.Count);
                int requiredAmount = recipeIngredient.Count;
                int ingredientPrice = ingredient.similarItem.Template.BasePrice;
                
                ingredientsCalculation += (additionalAmount / (float)requiredAmount) * (ingredientPrice * craftingFactor);
            }

            return ingredientsCalculation;
        }
        
        public static bool IsRecipeCraftable(IRecipe recipe, ICrafting crafting) {
            RecipeStatRequirement requirement = recipe.StatRequirement;
            bool meetsStatRequirement = true;
                
            if (requirement.statType != null) {
                meetsStatRequirement = Hero.Current.Stat(requirement.statType).ModifiedInt >= requirement.value;
            }
            
            bool meetsIngredientsRequirement = recipe.RecipeMatch(crafting.SimilarItemsData);
            return meetsIngredientsRequirement && meetsStatRequirement;
        }

        static bool IngredientMatch(ItemTemplate recipeIngredient, ItemTemplate modifiedIngredient) {
            if (modifiedIngredient == null) {
                return false;
            }
            
            if (recipeIngredient == modifiedIngredient) {
                return true;
            }

            return recipeIngredient.IsAbstract && 
                   modifiedIngredient.AbstractTypes.CheckContainsAndRelease(recipeIngredient);
        }
        
        static int CompareTemplatesToMatch(ItemTemplate a, ItemTemplate b) {
            if (a.IsAbstract) {
                if (b.IsAbstract) {
                    return String.CompareOrdinal(a.GUID, b.GUID);
                }
                return -1;
            }
            if (b.IsAbstract) {
                return 1;
            }
            return String.CompareOrdinal(a.GUID, b.GUID);
        }
    }

    public class IngredientCheckResult {
        public int missing;
        public int unused;
        [UnityEngine.Scripting.Preserve] public IEnumerable<Item> unusedItems;

        public IngredientCheckResult(int missing, List<Item> unusedItems) {
            this.missing = missing;
            this.unused = unusedItems.Count;
            this.unusedItems = unusedItems;
        }
    }
}