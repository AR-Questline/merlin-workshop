using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Crafting.HandCrafting.RecipeView;
using Awaken.TG.Main.Crafting.Slots;
using Awaken.TG.Main.Heroes.Items;
using Awaken.Utility.Collections;

namespace Awaken.TG.Main.Crafting.HandCrafting.IngredientsView {
    public partial class IngredientTabContents : IngredientTabs.Tab<VIngredientTabContents>, ICraftingTabRoot {
        public IngredientsGridUI IngredientsGridUI => ParentModel;
        public IngredientTabType TabType { get; }
        public int Index { get; set; }
        public bool IsEmpty => !IngredientsGridUI.ParentModel.Elements<InventorySlot>().Any();
        
        ExperimentalCooking ExperimentalCooking => IngredientsGridUI.ParentModel;
        
        public IngredientTabContents(IngredientTabType tabType) {
            TabType = tabType;
        }

        protected override void AfterViewSpawned(VIngredientTabContents view) {
            RefreshInventory();
        }
        
        IEnumerable<Item> GetFilteredItems() {
            var cookingItems = ExperimentalCooking.FilteredHeroItems.DistinctBy(x => x.Template);
            return cookingItems.Where(item => TabType.Contains(item));
        }

        public void RefreshInventory() {
            RefreshInventory(GetFilteredItems(), IngredientsGridUI.CurrentSorting);
            View<VIngredientTabContents>().Refresh();
            foreach (var slot in ExperimentalCooking.InventorySlots) {
                slot.Refresh();
            }
        }
        
        void RefreshInventory(IEnumerable<Item> filteredHeroItems, IngredientsSorting sorting) {
            ExperimentalCooking.RemoveElementsOfType<InventorySlot>();
            
            Index = 0;
            foreach (var item in filteredHeroItems.OrderWith(sorting)) {
                var similarItem = ExperimentalCooking.SimilarItemsData.First(x => x.Template == item.Template);
                var newSlot = new InventorySlot(Index++, new InteractableItem(similarItem, true));
                ExperimentalCooking.AddElement(newSlot);
            }
        }
    }
}