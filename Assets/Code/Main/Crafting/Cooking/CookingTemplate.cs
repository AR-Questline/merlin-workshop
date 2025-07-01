using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Sessions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Cooking {
    public class CookingTemplate : CraftingTemplate {
        public override IEnumerable<IRecipe> Recipes => _recipes.Get(this);
        public IEnumerable<GenericOutcome> GenericOutcomes => genericOutcomes;
        public int MinScoreForTasty => minScoreForTasty;
        public int MinScoreForDelicious => minScoreForDelicious;
        
        [SerializeField, ListDrawerSettings(HideAddButton = true)] 
        List<CookingRecipeGroup> recipeGroups;

        [SerializeField]
        List<GenericOutcome> genericOutcomes = new();

        [FoldoutGroup("Quality result")] 
        [SerializeField] int minScoreForTasty = 40;
        [FoldoutGroup("Quality result")] 
        [SerializeField] int minScoreForDelicious = 100;
        
        Cached<CookingTemplate, IRecipe[]> _recipes = new(static t => t.recipeGroups
            .SelectMany(rc => rc.Recipes)
            .Select(r => r.Get<IRecipe>())
            .ToArray());

        // === Editor only
#if UNITY_EDITOR
        
        List<TemplateReference> _helperRecipeList = new();
        [Button, Title("Editor Tools")]
        void SortGroups() {
            _helperRecipeList.AddRange(recipeGroups.SelectMany(x => x.Recipes));
            recipeGroups.Clear();
            _helperRecipeList.Sort(Comparison);

            CookingRecipeGroup group = null;
            foreach (TemplateReference recipe in _helperRecipeList) {
                ItemTemplate recipeOutcome = recipe.Get<IRecipe>().Outcome;
                if (group == null || group.Outcome != recipeOutcome) {
                    group = new(recipeOutcome);
                    recipeGroups.Add(group);
                }
                group.AddRecipe(recipe);
            }
            _helperRecipeList.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [Button]
        void DedupeRecipesWithinGroups() {
            foreach (CookingRecipeGroup group in recipeGroups) {
                group.DedupeRecipes();
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// This list is to allow for adding multiple FamilyHandcraftingTemplates at once. as TemplateReference currently doesn't support this.
        /// </summary>
        [ShowInInspector, HideInPlayMode, OnValueChanged(nameof(CustomAddFunction)), PropertyOrder(1), HideReferenceObjectPicker, ListDrawerSettings(HideAddButton = true)]
        List<CookingRecipe> _dragRecipesHere = new();

        void CustomAddFunction() {
            _helperRecipeList.AddRange(_dragRecipesHere.Select(r => new TemplateReference(GetAssetGUID(r))));
            SortGroups();
            _dragRecipesHere.Clear();
        }
#endif
    }
    
    [Serializable]
    public struct GenericOutcome {
        [SerializeField, TemplateType(typeof(ItemTemplate))]
        TemplateReference outcomeRef;
        [SerializeField]
        int minScore;
        
        public ItemTemplate OutcomeTemplate => outcomeRef.Get<ItemTemplate>();
        public int MinScore => minScore;
    }
}