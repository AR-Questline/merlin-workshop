using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting.RecipeView {
    public partial class RecipeGridUI : Element<IRecipeCrafting>, RecipeTabs.ITabParent<VRecipeGridUI> {
        public sealed override bool IsNotSaved => true;

        public Transform DeterminedHost => RecipeCrafting.GridUIParent;
        public IRecipeCrafting RecipeCrafting => ParentModel;
        
        public IEnumerable<IRecipe> AllRecipesOfCurrentType => AllRecipes.Where(r => !r.IsHidden && CurrentType.Contains(r));
        public Prompts Prompts => RecipeCrafting.Prompts;

        public RecipeTabContents CurrentTab => Element<RecipeTabContents>();
        public IRecipe SelectedRecipe => SelectedRecipeSlot?.Recipe;

        public List<IRecipe> AllRecipes { get; private set; }
        public RecipeTabType CurrentType { get; set; }
        public Tabs<RecipeGridUI, VRecipeTabs, RecipeTabType, RecipeTabContents> TabsController { get; set; }
        public RecipeSlot SelectedRecipeSlot { get; private set; }
        public RecipeSorting CurrentSorting { get; private set; } = RecipeSorting.AlphabeticallyAscending;

        public new static class Events {
            public static readonly Event<RecipeGridUI, IRecipe> SelectedRecipeChanged = new(nameof(SelectedRecipeChanged));
        }

        public RecipeGridUI(IEnumerable<IRecipe> availableRecipes) {
            AllRecipes = availableRecipes.ToList();
        }

        protected override void OnFullyInitialized() {
            World.SpawnView<VRecipeGridUI>(this, true, true, DeterminedHost);
            AddElement(new RecipeTabs(RecipeCrafting.AllowedTabTypes()));
            ParentModel.ShowEmptyInfo(!CurrentTab.IsEmpty);
        }

        public void Refresh(bool contentsChanged = false) {
            IEnumerable<IRecipe> recipes = AllRecipes;
            bool force = false;
            if (ParentModel.HasRecipesChanged(ref recipes)) {
                AllRecipes = recipes.ToList();
                force = true;
            }
            CurrentTab.Refresh(force, contentsChanged);
        }
        
        public void ChangeItemsComparer(RecipeSorting sorting) {
            if (CurrentSorting == sorting) return;
            
            CurrentSorting = sorting;
            ClickSlot(null);
            TriggerChange();
            Refresh();
        }

        public void ClickSlot(RecipeSlot slot) {
            if (SelectedRecipeSlot == slot) {
                return;
            }
            
            SelectedRecipeSlot?.UnselectSlot();
            SelectedRecipeSlot = slot;
            this.Trigger(Events.SelectedRecipeChanged, SelectedRecipe);
        }
    }
}