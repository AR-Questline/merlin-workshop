using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting.RecipeView {
    [UsesPrefab("Crafting/Handcrafting/" + nameof(VRecipeTabContents))]
    public class VRecipeTabContents : View<RecipeTabContents> {
        [SerializeField] Transform recipesHost;
        [SerializeField] VItemsListRecipeUI recipeListUI;
        
        CanvasGroup _fadeIn;
        RecipeSorting _prevSorting;
            
        public Transform RecipeHost => recipesHost;

        protected override void OnInitialize() {
            Target.ListenTo(RecipeTabContents.Events.ContentsUpdated, Refresh, this);
        }

        public void Refresh(bool contentsChanged) {
            var newSorting = Target.RecipeGridUI.CurrentSorting;
            
            if (contentsChanged || _prevSorting == null || _prevSorting != newSorting) {
                SortChildren(newSorting);
                _prevSorting = newSorting;
            }
        }

        void SortChildren(RecipeSorting currentSorting) {
            recipeListUI.Refresh();
            var recipeSlots = Target.Elements<RecipeSlot>();

            if (!recipeSlots.Any()) {
                Target.ClickRecipe(null);
                return;
            }

            int newIndex = 0;
            RecipeSlot currentlySelected = null;

            var orderedSlots = recipeSlots.GetManagedEnumerator()
                .OrderBy(slot => !CraftingUtils.IsRecipeCraftable(slot.Recipe, Target.ParentModel.ParentModel))
                .ThenBy(slot => slot.Recipe, currentSorting)
                .ThenBy(slot => slot.Recipe.Outcome, ItemTemplateTypeComparer.Comparer)
                .ThenBy(slot => slot.Recipe, RecipeSorting.AlphabeticallyAscending)
                .ToList();
            
            foreach (RecipeSlot slot in orderedSlots) {
                if (slot.IsSelected) {
                    slot.UnselectSlot();
                    currentlySelected = slot.IsCraftable ? slot : orderedSlots.LastOrDefault(s => s.IsCraftable && s.Index <= slot.Index);
                }
                currentlySelected ??= slot;
                slot.RefreshIndex(newIndex++);
            }

            recipeListUI.OrderChanged();

            SelectAfterDelay(currentlySelected).Forget();
        }
        
        async UniTaskVoid SelectAfterDelay(RecipeSlot slotToSelect) {
            if (await AsyncUtil.DelayFrame(Target, 3)) {
                slotToSelect.SelectSlot();
            }
        }
    }
}