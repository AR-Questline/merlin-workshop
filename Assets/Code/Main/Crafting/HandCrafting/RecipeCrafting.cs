using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.HandCrafting.RecipeView;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Crafting.Result;
using Awaken.TG.Main.Crafting.Slots;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting {
    public abstract partial class RecipeCrafting<T> : Crafting<T>, IRecipeCrafting where T : CraftingTemplate {
        
        RecipeGridUI RecipeGridUI { get; set; }
        public virtual bool HasItemLevelPreviewer => true;
        public Prompts Prompts => ParentModel.Prompts;
        public Transform GridUIParent => View<VRecipeCrafting>().gridUIParent;
        public override bool ButtonInteractability => CurrentRecipe != null && RecipeGridUI.SelectedRecipeSlot.IsCraftable;
        VCItemLevelOutcomesInfo ItemLevelOutcomesInfo => View<VRecipeCrafting>().ItemLevelOutcomesInfo;
        protected override bool KnownRecipe(IRecipe recipe) => Hero.Element<HeroRecipes>().IsLearned(recipe);
        
        public abstract IEnumerable<RecipeTabType> AllowedTabTypes();

        public override EventReference CraftCompletedSound => CommonReferences.Get.AudioConfig.CraftingAudio.GetResultSound(_recentCraftingResult.quality);
        
        protected RecipeCrafting(Hero hero, T genericTemplate) : base(hero, genericTemplate) { }

        // === Overrides
        public override void Init() {
            RecipeGridUI = AddElement(new RecipeGridUI(Recipes));
            RecipeGridUI.ListenTo(RecipeGridUI.Events.SelectedRecipeChanged, OnRecipeChange, this);
            RefreshWorkbenchSlots();
        }

        protected override CraftingSlot CreateWorkbenchSlot() {
            return new EditableWorkbenchSlot();
        }

        protected override void OnWorkbenchSlotsChange() {
            UpdateButtonInteractability();
        }

        protected override CraftingResult DetermineCraftingResult() {
            if (CurrentRecipe.CanHaveItemLevel && ItemLevelOutcomesInfo != null) {
                var itemLevel = ItemLevelOutcomesInfo.CurrentDrawnItemLevel;
                var quality = ItemLevelOutcomesInfo.CurrentDrawnItemLevelQuality;
                ItemLevelOutcomesInfo.RefreshCurrentItemLevel();
                return new CraftingResult(CurrentRecipe.Create(this, itemLevel), quality);
            }

            return base.DetermineCraftingResult();
        }

        protected override void AfterCreate(Item item) {
            base.AfterCreate(item);
            ParentModel.AddElement(new ItemDiscoveredInfo(new [] {item})).ShowItemCreatedInfo(item.DisplayName);
            GenerateRecipe(CurrentRecipe);
            RecipeGridUI.Refresh(true);
        }

        public override void AddToWorkbenchSlot(InteractableItem interactableItem) { }
        
        public bool HasRecipesChanged(ref IEnumerable<IRecipe> recipes) {
            if (Recipes.SequenceEqual(recipes)) {
                return false;
            }

            recipes = Recipes;
            return true;
        }

        /// <summary>
        /// Checks if new recipe is valid and if it is sets up crafting state
        /// </summary>
        void OnRecipeChange(IRecipe recipe) {
            CurrentRecipe = recipe;
            
            if (CurrentRecipe != null) {
                GenerateRecipe(CurrentRecipe);
                this.Trigger(IRecipeCrafting.Events.OnRecipeChanged, CurrentRecipe);
            } else {
                foreach (WorkbenchSlot ingredientSlot in WorkbenchSlots) {
                    ingredientSlot.RemoveElementsOfType<GhostItem>();
                }
            }
            UpdateButtonInteractability();
        }
        
        /// <summary>
        /// Generates a recipe in ingredient slots
        /// </summary>
        /// <param name="recipe">the recipe to base the generated items from</param>
        void GenerateRecipe(IRecipe recipe) {
            int ingredientsCount = recipe.Ingredients.Length;

            foreach (var (index, ingredientSlot) in WorkbenchSlots.GetIndexedEnumerator()) {
                if (index < ingredientsCount) {
                    continue;
                }
                ingredientSlot.RemoveElementsOfType<GhostItem>();
                ingredientSlot.UpdateSlot(null, 0, false);
                ingredientSlot.MainView.gameObject.SetActive(false);
            }

            var workbenchSlotsEnumerator = WorkbenchSlots.GetEnumerator();
            for (int i = 0; i < ingredientsCount; i++) {
                workbenchSlotsEnumerator.MoveNext();

                Ingredient currentIngredient = recipe.Ingredients[i];
                var ingredientSlot = workbenchSlotsEnumerator.Current;
                var existingGhostItem = ingredientSlot.TryGetElement<GhostItem>();

                ItemTemplate wantedItemTemplate = currentIngredient.Template;
                SimilarItemsData similarItemsData = SimilarItemsData.FirstOrDefault(x => x.Template.InheritsFrom(currentIngredient.Template));

                if (existingGhostItem != null) {
                    //Preventing element regeneration flicker
                    existingGhostItem.ChangeProperties(
                        wantedItemTemplate,
                        currentIngredient.Count,
                        similarItemsData);
                } else {
                    // Generate new
                    ingredientSlot.AddElement(
                        new GhostItem(
                            wantedItemTemplate,
                            currentIngredient.Count,
                            similarItemsData));
                }

                bool canChangeQuantity = recipe.CanHaveItemLevel && ButtonInteractability;
                ingredientSlot.UpdateSlot(currentIngredient, similarItemsData.Quantity, canChangeQuantity);
                ingredientSlot.MainView.gameObject.SetActive(true);
            }
        }

        public override void OverrideLabels(IEmptyInfo infoView) {
            infoView.EmptyInfoView.SetupLabels(LocTerms.EmptyCraftingNoItems.Translate(), LocTerms.EmptyCraftingGatherMaterials.Translate(), LocTerms.EmptyCraftingGatherMaterialsDesc.Translate());
        }

        void UpdateButtonInteractability() => TriggerChange();
    }
}