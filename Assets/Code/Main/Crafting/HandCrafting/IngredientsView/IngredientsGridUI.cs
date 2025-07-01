using System.Collections.Generic;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Crafting.HandCrafting.IngredientsView {
    [SpawnsView(typeof(VIngredientGridUI))]
    public partial class IngredientsGridUI : Element<ExperimentalCooking>, IngredientTabs.ITabParent<VIngredientGridUI> {
        public IngredientTabType CurrentType { get; set; }
        public Tabs<IngredientsGridUI, VIngredientTabs, IngredientTabType, IngredientTabContents> TabsController { get; set; }

        public IEnumerable<IngredientTabType> Tabs => IngredientTabType.ExperimentalCooking;
        public IEnumerable<Item> Items => ParentModel.FilteredHeroItems;
        public IngredientsSorting CurrentSorting { get; private set; } = IngredientsSorting.AlphabeticallyAscending;
        public IngredientTabContents IngredientTabContents => Element<IngredientTabContents>();

        protected override void OnFullyInitialized() {
            AddElement(new IngredientTabs());
        }
        
        public void ChangeItemsComparer(IngredientsSorting sorting) {
            if (CurrentSorting == sorting) return;
            CurrentSorting = sorting;
            IngredientTabContents.RefreshInventory();
        }
    }
}