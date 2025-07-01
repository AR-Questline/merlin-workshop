using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Crafting.HandCrafting.IngredientsView;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Crafting.Result;
using Awaken.TG.Main.Crafting.Slots;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.Cooking {
    public partial class ExperimentalCooking : Crafting<CookingTemplate>, ICraftingTabContents {
        public const string RequiredKind = "cook";

        public sealed override bool IsNotSaved => true;
        
        public override Type TabView => typeof(VExperimentalCooking);

        public override Func<Item, bool> ItemFilter => item => TagUtils.HasRequiredKind(item.Tags, RequiredKind);
        public override float TooltipTweenTime => 0.5f;
        public override bool TooltipPreventDisappearing => false;
        public override bool ButtonInteractability => CurrentRecipe != null;
        public bool WorkbenchHasItems => WorkbenchItemsData.Any();
        public override EventReference CraftCompletedSound {
            get {
                int score = CalculateScore(WorkbenchCraftingItems);
                if (score < Template.MinScoreForTasty) {
                    return CommonReferences.Get.AudioConfig.CraftingAudio.CraftingResultPoor;
                }
                if (score < Template.MinScoreForDelicious) {
                    return CommonReferences.Get.AudioConfig.CraftingAudio.CraftingResultRegular;
                }
                return CommonReferences.Get.AudioConfig.CraftingAudio.CraftingResultGreat;
            } 
        }

        public override Transform InventoryParent => Element<IngredientsGridUI>().Element<IngredientTabContents>()
            .View<VIngredientTabContents>().IngredientsHost;

        public ExperimentalCooking(Hero hero, CookingTemplate template) : base(hero, template) { }

        protected override bool KnownRecipe(IRecipe recipe) => true;

        bool _randomizationModeEnabled = false;

        public override void Init() {
            base.Init(); 
            var ingredientsGridUI = AddElement(new IngredientsGridUI());
            ShowEmptyInfo(!ingredientsGridUI.IngredientTabContents.IsEmpty);
        }

        void PutRandomIngredients() {
            var workbenchSlotsCount = 0;
            foreach (var slot in WorkbenchSlots) {
                slot.Submit();
                workbenchSlotsCount++;
            }
            
            for (int i = 0; i < workbenchSlotsCount; i++) {
                var items = InventorySlots.Where(s => s is { HasBeenDiscarded: false, Item: { HasBeenDiscarded: false }}).ToArray();
                if (!items.Any()) {
                    return;
                }
                
                var randomInventorySlot = RandomUtil.WeightedSelect(items, slot => slot.Item.Quantity);
                randomInventorySlot.Submit();
            }
        }

        protected override void OnWorkbenchSlotsChange() {
            if (!WorkbenchItemsData.Any()) {
                CurrentRecipe = null;
                _randomizationModeEnabled = false;
            } else {
                CurrentRecipe = Recipes.FirstOrDefault(recipe => recipe.ExperimentalMatch(WorkbenchItemsData));
                if (CurrentRecipe == null) {
                    // Create "Food" recipe
                    int score = CalculateScore(WorkbenchCraftingItems);
                    GenericOutcome outcome = Template.GenericOutcomes.Where(gr => score >= gr.MinScore).MaxBy(gr => gr.MinScore, true);
                    GenericOutcomeCookingRecipe recipe = new(outcome.OutcomeTemplate);
                    CurrentRecipe = recipe;
                }
            }

            TriggerChange();
        }
        
        int CalculateScore(IEnumerable<CraftingItem> workbenchItems) {
            float ingredientsScore = workbenchItems.Sum(i => i.Item.GetHealValue());
            if (ingredientsScore <= 4) {
                ingredientsScore = 5;
            } else if (ingredientsScore <= 7) {
                ingredientsScore = 10;
            } else {
                ingredientsScore = 15;
            }
            float proficiency = Hero.ProficiencyStats.Cooking.ModifiedInt;

            int proficiencyModifier = Mathf.FloorToInt((proficiency + 10) / 10f);
            float score = ingredientsScore * proficiencyModifier;
            return Mathf.CeilToInt(score);
        }

        protected override void AfterCreate(Item item) {
            base.AfterCreate(item);
            _currentSlot.RemoveAllWorkbenchItems();
            RefreshTooltipDescriptor(item.Level.ModifiedInt);

            if (CurrentRecipe is not GenericOutcomeCookingRecipe) {
                if (!IsLearned(CurrentRecipe)) {
                    ParentModel.AddElement(new ItemDiscoveredInfo(new[] {item})).ShowNewRecipeDiscoveredInfo(item.DisplayName);
                }
                DiscoverRecipe(CurrentRecipe);
                Hero.Development.RewardExpAsPercentOfNextLevel(Services.Get<GameConstants>().RecipeLearnExpMulti);
                Hero.ProficiencyStats.TryAddXP(ProfStatType.Cooking, Services.Get<GameConstants>().RecipeLearnCraftingExp);
            }
            
            ShowEmptyInfo(!Element<IngredientsGridUI>().IngredientTabContents.IsEmpty);

            if (_randomizationModeEnabled) {
                PutRandomIngredients();
            }
            
            OnWorkbenchSlotsChange();
        }

        public bool TryReceiveFocus() => true;
        
        public void RandomizeIngredients() {
            PutRandomIngredients();
            _randomizationModeEnabled = true;
        }
        
        public override void OverrideLabels(IEmptyInfo infoView) {
            infoView.EmptyInfoView.SetupLabels(LocTerms.EmptyCraftingNoIngredients.Translate(), LocTerms.EmptyCraftingGatherMaterials.Translate(), LocTerms.EmptyCraftingGatherMaterialsDesc.Translate());
        }
    }
}