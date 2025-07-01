using Awaken.TG.Main.UI.Components;
using Awaken.TG.MVC;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    public abstract class VBaseItemsListUI : View<ItemsListUI>, IItemsList {
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
        
        public int? ItemsCount { get; set; }
        public int? LastItemIndex { get; set; }
        public int? FirstItemIndex { get; set; }
        public int DisplayColumnCount => ListAdjuster.CalculateDisplayColumnCount(MaxColumnCount);
        public float ItemListWidth => _itemListParentRect.rect.width;
        
        RectTransform _itemListParentRect;

        void Awake() {
            _itemListParentRect = ItemListParent.GetComponent<RectTransform>();
        }

        protected override void OnInitialize() {
            ItemListParent.SetActive(false);
            NoItemsParent.SetActive(false);
        }
        
        public void Refresh() {
            ConfigureList();
        }

        public void RefreshSelfState() {
            bool isEmpty = ItemsCount is 0;
            gameObject.SetActiveOptimized(!isEmpty);
        }
        
        public void OrderChanged() {
            ListAdjuster.SizeAdjust(MaxRowCount, MaxColumnCount).Forget();
            GridManager.OrderChangedRefresh();
            RefreshParent();
        }

        public void ChangeGrid(int columnCount = -1, int rowCount = -1) {
            if (columnCount != -1) MaxColumnCount = columnCount;
            if (rowCount != -1) MaxRowCount = rowCount;
        }

        protected virtual void ConfigureList() {
            ListAdjuster.FullAdjustWithCollectionRefresh(MaxRowCount, MaxColumnCount).Forget();
            RefreshParent();
        }
        
        void RefreshParent() {
            NoItemsParent.SetActive(Target.IsEmpty);
            ItemListParent.SetActive(!Target.IsEmpty);
        }
    }
}