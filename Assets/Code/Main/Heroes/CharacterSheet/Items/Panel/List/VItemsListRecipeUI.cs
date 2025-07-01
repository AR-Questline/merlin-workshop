using Awaken.TG.Main.Crafting.HandCrafting.RecipeView;
using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    public class VItemsListRecipeUI : ViewComponent<ICraftingTabRoot>, IItemsList {
        [field: Title("General Settings")]
        [field: SerializeField] public Transform ItemHost { get; private set; }
        [field: SerializeField] public int MaxColumnCount { get; protected set; } = 7;
        [field: SerializeField] public int MaxRowCount { get; protected set; } = 5;
        
        [field: Title("Items Hosts")]
        [field: SerializeField] GameObject ItemListParent { get; set; }
        [field: SerializeField] GameObject NoItemsParent { get; set; }
        
        [field: Title("Tools")]
        [field: SerializeField] RecyclableCollectionManager GridManager { get; set; }
        [field: SerializeField] VCListAdjuster ListAdjuster { get; set; }
        
        [Title("Sorting")]
        [SerializeField] GameObject sortingParent;
        
        public int? ItemsCount { get; set; }
        public int? LastItemIndex { get; set; }
        public int? FirstItemIndex { get; set; }
        public int DisplayColumnCount => ListAdjuster.CalculateDisplayColumnCount(MaxColumnCount);

        protected override void OnAttach() {
            ConfigureList();
        }
        
        public void Refresh() {
            ConfigureList();
        }
        
        public void RefreshSelfState() {
            bool isEmpty = ItemsCount is 0;
            gameObject.SetActiveOptimized(!isEmpty);
        }
        
        public void OrderChanged() => GridManager.OrderChangedRefresh();
        
        public void ChangeGrid(int columnCount = -1, int rowCount = -1) {
            if (columnCount != -1) MaxColumnCount = columnCount;
            if (rowCount != -1) MaxRowCount = rowCount;
        }

        protected virtual void ConfigureList() {
            ItemListParent.SetActive(false);
            NoItemsParent.SetActive(false);
            
            ListAdjuster.FullAdjustWithCollectionRefresh(MaxRowCount, MaxColumnCount).Forget();

            NoItemsParent.SetActive(Target.IsEmpty);
            ItemListParent.SetActive(!Target.IsEmpty);
            sortingParent.SetActive(!Target.IsEmpty);
        }
    }
}