using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting.RecipeView {
    public interface ICraftingTabRoot : IModel {
        bool IsEmpty { get; }
    }
    
    public partial class RecipeTabContents : RecipeTabs.Tab<VRecipeTabContents>, ICraftingTabRoot {
        RecipeTabType TabType { [UnityEngine.Scripting.Preserve] get; }

        public Transform RecipeHost => View<VRecipeTabContents>().RecipeHost;
        public ModelsSet<RecipeSlot> RecipeSlots => Elements<RecipeSlot>();
        public RecipeGridUI RecipeGridUI { get; }
        public bool IsEmpty => !RecipeSlots.Any();
        
        Prompt _usePrompt;
        
        public new static class Events {
            public static readonly Event<RecipeTabContents, bool> ContentsUpdated = new(nameof(ContentsUpdated));
        }

        public RecipeTabContents(RecipeTabType tabType, RecipeGridUI recipeGridUI) {
            TabType = tabType;
            RecipeGridUI = recipeGridUI;
        }

        protected override void OnFullyInitialized() {
            _usePrompt = RecipeGridUI.Prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.Confirm.Translate()), this, false);
        }

        protected override void AfterViewSpawned(VRecipeTabContents view) {
            GenerateAllSlotsForFilteredRecipes();
        }

        void GenerateAllSlotsForFilteredRecipes(bool force = false, bool contentsUpdated = false) {
            if (force) {
                RemoveElementsOfType<RecipeSlot>();
            }
            
            if (force || !Elements<RecipeSlot>().Any()) {
                int index = 0;
                foreach (var recipe in RecipeGridUI.AllRecipesOfCurrentType) {
                    AddElement(new RecipeSlot(recipe, index++));
                }
            }
            this.Trigger(Events.ContentsUpdated, contentsUpdated);
            TryReceiveFocus();
        }

        public void ClickRecipe(RecipeSlot slot) {
            _usePrompt.SetActive(slot != null);
            RecipeGridUI.ClickSlot(slot);
        }

        public void Refresh(bool force = false, bool contentsChanged = false) {
            GenerateAllSlotsForFilteredRecipes(force, contentsChanged);
        }

        public override bool TryReceiveFocus() {
            Component target = null;
            RecipeSlot previousTarget = RecipeSlots.FirstOrDefault(slot => slot.Recipe == ParentModel.SelectedRecipe);
            
            if (previousTarget) {
                target = previousTarget.View<VRecipeSlot>()?.FocusTarget;
            } else if (RecipeHost.TryGetComponentInChildren(out VRecipeSlot slot)) {
                target = slot.FocusTarget;
            }
            
            if (target) {
                FocusTarget(target).Forget();
                return true;
            }
            return false;
        }
        
        async UniTaskVoid FocusTarget(Component focusTarget) {
            if (await AsyncUtil.DelayFrame(this)) {
                World.Only<Focus>().Select(focusTarget);
            }
        }
    }
}