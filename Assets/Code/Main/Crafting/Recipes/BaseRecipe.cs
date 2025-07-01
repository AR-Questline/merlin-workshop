using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.Utility.Sessions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Recipes {
    public abstract class BaseRecipe : Template, IRecipe {
        public const int GarbageItemThreshold = ItemTemplate.MinimumItemLevel - 1;

        [BoxGroup("Difficulty"), SerializeField] 
        [Tooltip("Enable this option to restrict crafting to default (level 0) items only. " +
                  "Additional item crafting, which would otherwise balance those without levels, is also disabled.")]
        bool disableItemLevels;
        [BoxGroup("Difficulty"), SerializeField, HideIf(nameof(disableItemLevels))] 
        float itemCraftingDifficulty;
        [BoxGroup("Difficulty"), SerializeField, RichEnumExtends(typeof(HeroStatType))]
        RichEnumReference statRequirement;
        [BoxGroup("Difficulty"), SerializeField, ShowIf(nameof(IsStatRequirementSet))] 
        int statRequirementValue;
        [BoxGroup("Templates"), SerializeField, Space]
        Ingredient[] ingredients = Array.Empty<Ingredient>();
        [BoxGroup("Templates"), TemplateType(typeof(ItemTemplate)), Space]
        public TemplateReference outcome;
        [BoxGroup("Templates")]
        public int quantity = 1;
        [SerializeField, Space(15)] bool isHidden;
        [SerializeField, TemplateType(typeof(StoryGraph))] TemplateReference storyOnCreation;

        TemplateType ITemplate.TemplateType {
            get {
                if (templateType == TemplateType.Regular) {

                    if (Outcome != null){
                        TemplateType outcomeType = Outcome.templateType;

                        if (outcomeType != TemplateType.Regular) {
                            return Outcome.templateType;
                        }
                    }

                    if (Ingredients.Length == 0) return TemplateType.Regular;

                    return Ingredients
                        .Select(i => i.Template.templateType)
                        .OrderBy(t => t)
                        .Last();
                }
                return templateType;
            }
        }

        Cached<BaseRecipe, Ingredient[]> _ingredients = new(static recipe => recipe.ingredients.Where(i => i?.Template != null).ToArray());
        Cached<BaseRecipe, string> _translation = new(static recipe => recipe.Outcome.ItemName);

        bool IsStatRequirementSet => statRequirement.EnumAs<HeroStatType>() is not null;
        
        public bool IsHidden => isHidden;
        public TemplateReference StoryOnCreation => storyOnCreation;
        public Ingredient[] Ingredients => _ingredients.Get(this);
        Ingredient[] IRecipe.Ingredients => Ingredients;
        public ItemTemplate Outcome => outcome.Get<ItemTemplate>();
        public int Quantity => quantity;
        public bool CanHaveItemLevel => !disableItemLevels && Outcome.CanHaveItemLevel;
        public float ItemCraftingDifficulty => itemCraftingDifficulty;
        public RecipeStatRequirement StatRequirement => new(statRequirement.EnumAs<HeroStatType>(), statRequirementValue);
        
        public abstract ProfStatType ProficiencyStat { get; }
        public abstract HeroStatType BonusLevelStat { get; }
        protected abstract ItemTemplate GarbageItem { get; }
        
        public virtual bool ExperimentalMatch(IEnumerable<ItemData> items) {
            return CraftingUtils.MultipleItemsMatchIngredients(Ingredients, items);
        }

        public virtual bool RecipeMatch(IEnumerable<SimilarItemsData> items) {
            return CraftingUtils.DistinctItemsMatchIngredientsWithQuantity(Ingredients, items);
        }

        public Item Create(ICrafting crafting = null, int? overridenLevel = null) {
            int itemLvl = overridenLevel ?? Mathf.FloorToInt(CraftingUtils.GetItemLevelForCrafted(this));
            int additionalItems = CalculateAdditionalItems(ref itemLvl) + 1;
            return CreateItem(itemLvl, quantity * additionalItems);
        }

        Item CreateItem(int itemLvl, int finalQuantity) {
            bool willCreateGarbage = GarbageItem != null && itemLvl <= GarbageItemThreshold;
            return willCreateGarbage ? new(GarbageItem) : new(Outcome, finalQuantity, itemLvl);
        }

        public string DisplayString() {
            return string.Join("+", Ingredients.Select(i => i.Template.ItemName)) + " => " + outcome?.Get<ItemTemplate>().ItemName;
        }

        public string OutcomeName() {
            return _translation.Get(this);
        }

        public void StartStoryOnCreation() {
            if (StoryBookmark.ToInitialChapter(StoryOnCreation, out var bookmark)) {
                Story.StartStory(StoryConfig.Base(bookmark, null));
            }
        }

        int CalculateAdditionalItems(ref int itemLvl) {
            int additionalItems = 0;
            
            if (disableItemLevels) {
                itemLvl = 0;
            } else if (!CanHaveItemLevel) {
                if(Outcome.canStack && itemLvl > 0) {
                    additionalItems = RandomUtil.UniformInt(0, itemLvl);
                }
                itemLvl = 0;
            }
            return additionalItems;
        }

        // === Editor
#if UNITY_EDITOR
        [Button("Sort Ingredients")]
        public void EDITOR_SortIngredients() {
            CraftingUtils.SortIngredients(ingredients);
        }

        [Button("Learn"), EnableIf(nameof(EDITOR_CanUseContexts))]
        void EDITOR_LearnRecipe() {
            TemplatesUtil.EDITOR_AssignGuid(this, gameObject);
            Hero.Current.Element<HeroRecipes>().LearnRecipe(this);
        }

        bool EDITOR_CanUseContexts() {
            return Hero.Current != null;
        }

        public void Editor_SetIngredients(IEnumerable<Ingredient> newIngredients) {
            ingredients = newIngredients.ToArray();
        }
#endif
    }
}
