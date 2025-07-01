using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List {
    public partial class ItemsListUI : ItemsTabs.Tab<VHostItemsList> {
        ItemsTabType _filterOverride;
        ItemsSorting _sortingOverride;
        int CurrentIndex { get; set; }
        int ColumnCount { get; set; }

        IEnumerable<Item> Items => ItemsUI.Items.Where(TabType.Contains);
        ModelsSet<ItemsListElementUI> ItemsListElements => Elements<ItemsListElementUI>();
        ItemsSorting CurrentSorting => _sortingOverride ?? ItemsUI.GetCurrentSorting(ParentModel.Config.SortingTab ?? TabType);
        ItemsTabType CurrentFilter => _filterOverride ?? ItemsUI.GetCurrentTabFilter(TabType);
        ItemsTabType CurrentType => ParentModel.Config.SortingTab ?? TabType;

        public ItemsListElementUI HoveredItemsListElement => GetItemsListElementWithItem(HoveredItem);
        public ItemsListElementUI ClickedItemsListElement => GetItemsListElementWithItem(ClickedItem);
        public ItemsTabType TabType { get; }
        public Item ClickedItem { get; private set; }
        public Item HoveredItem { get; private set; }
        public ItemsUI ItemsUI => ParentModel;
        public bool IsEmpty => !Items.Any();
        public bool IsMultipleLists => ItemsByTab.Count > 1;

        Dictionary<ItemsTabType, (IItemsList listView, List<Item> items)> ItemsByTab { get; set; }
        List<ItemsTabType> _subTabsInOrder;
        bool _onlyOneList;
        VHostItemsListWithCategory _viewHostWithCategory;
        Transform _viewHost;
        
        public ItemsListUI(ItemsTabType tabType) {
            TabType = tabType;
        }

        protected override void AfterViewSpawned(VHostItemsList view) {
            _viewHost = view.ViewHost;
            SetupItemByTabs().Forget();
        }
        
        //TODO: better handle of multiple recyclable collections/lists and ride of the linq queries
        protected async UniTaskVoid SetupItemByTabs() {
            bool addedAnyTab = false;
            ItemsTabType[] subTabs = CurrentType.SubTabs;
            string tabTitle = CurrentType.Title;
            
            if (subTabs is { Length: > 1 }) {
                _subTabsInOrder = new List<ItemsTabType>(subTabs) { ItemsTabType.None };
            }
            
            ItemsByTab = new Dictionary<ItemsTabType, (IItemsList listView, List<Item> items)>();
            
            if (UseDefaultList(CurrentType)) {
                if (World.SpawnView(this, ParentModel.Config.ItemsListUIView, false, true, _viewHost) is IItemsList itemsWithoutSubTabs) {
                    SetupItemsList(CurrentType, itemsWithoutSubTabs, Items.ToList(), tabTitle);
                }
                _onlyOneList = true;
                ItemsByTab[CurrentType].listView.FirstItemIndex = 0;
                ItemsByTab[CurrentType].listView.LastItemIndex = Items.Count() - 1;
            } else {
                _viewHostWithCategory = null;
                foreach (var subTab in subTabs) {
                    var itemsInSubTab = Items.Where(subTab.ContainsInGrid).ToList();
                    if (subTab == ItemsTabType.All || subTab == ItemsTabType.None || !itemsInSubTab.Any()) {
                        continue;
                    }
                    
                    if (_viewHostWithCategory == null) {
                        _viewHostWithCategory = World.SpawnView(this, ParentModel.Config.ItemsCategoryListHostView, false, true, _viewHost) as VHostItemsListWithCategory;
                    }
                    
                    if (World.SpawnView(this, ParentModel.Config.ItemsCategoryListUIView, false, true, _viewHostWithCategory!.ViewHost) is IItemsList itemsSubTabView) {
                        SetupItemsList(subTab, itemsSubTabView, itemsInSubTab, subTab.Title);
                        SetupItemsListWithSubTab(itemsSubTabView, itemsInSubTab.Count);
                    }
                }
                
                var usedItems = subTabs.Where(subTab => subTab != ItemsTabType.All && subTab != ItemsTabType.None).SelectMany(subTab => Items.Where(subTab.ContainsInGrid));
                var otherItems = Items.Except(usedItems).ToList();
                int otherItemsCount = otherItems.Count;

                if (otherItemsCount > 0) {
                    if (_viewHostWithCategory == null) {
                        _viewHostWithCategory = World.SpawnView(this, ParentModel.Config.ItemsCategoryListHostView, false, true, _viewHost) as VHostItemsListWithCategory;
                    }
                    
                    if (World.SpawnView(this, ParentModel.Config.ItemsCategoryListUIView, false, true, _viewHostWithCategory!.ViewHost) is IItemsList otherItemsView) {
                        SetupItemsList(ItemsTabType.None, otherItemsView, otherItems, LocTerms.OtherItems.Translate());
                        SetupItemsListWithSubTab(otherItemsView, otherItemsCount);
                    }
                }
            }
            
            if (addedAnyTab && await AsyncUtil.DelayFrame(this)) {
                RefreshGridSize();
                Refresh();
            }
            
            // 3 frames delay to ensure VCListAdjuster executed two one-frame delays and the grid size is set and items are initialized
            if (_viewHostWithCategory != null && await AsyncUtil.DelayFrame(this, 3)) {
                _viewHostWithCategory.ResizeToContent(View<VBaseItemsListUI>().ItemListWidth);
            }

            return;

            void SetupItemsList(ItemsTabType tabType, IItemsList itemsList, List<Item> items, string title) {
                addedAnyTab = true;
                ItemsByTab.Add(tabType, (itemsList, items));
                
                if (itemsList is IItemsListTitle itemsListTitle) {
                    itemsListTitle.SetupTitle(title);
                } else {
                    ParentModel.SetupTitle();
                }
            }

            void SetupItemsListWithSubTab(IItemsList itemsList, int count) {
                itemsList.ItemsCount = count;
                ParentModel.SetupTitle();
            }
        }
        
        bool UseDefaultList(ItemsTabType mainType) => mainType.SubTabs == null 
                                                      || mainType == ItemsTabType.All 
                                                      || mainType == ItemsTabType.None 
                                                      || !ParentModel.Config.UseCategoryList;

        public void Refresh() {
            if (HasElement<ItemsListElementUI>()) {
                RemoveElementsOfType<ItemsListElementUI>();
            }
            
            if (HasBeenDiscarded || ItemsByTab.IsEmpty()) {
                return;
            }

            int index = 0;
            
            if (IsMultipleLists) {
                Items.Where(item => CurrentFilter?.Contains(item) ?? true)
                    .GroupBy(item => ItemsByTab.FirstOrDefault(pair => pair.Value.items.Contains(item)).Key)
                    .OrderBy(group => _subTabsInOrder.IndexOf(group.Key))
                    .SelectMany(group => group.OrderWith(CurrentSorting)
                        .Select(item => new { Item = item, CurrentType = group.Key }))
                    .ForEach(item => {
                        ItemsByTab[item.CurrentType].listView.FirstItemIndex ??= index;
                        ItemsByTab[item.CurrentType].listView.LastItemIndex = index;
                        var itemsListElementUI = new ItemsListElementUI(item.Item, index++, GetViewParentForItem(item.Item));
                        InitListElement(itemsListElementUI);
                    });
            } else {
                var itemsToDisplay = Items.Where(item => CurrentFilter?.Contains(item) ?? true)
                    .OrderWith(CurrentSorting).ToList();
                var listView = ItemsByTab.FirstOrDefault().Value.listView;

                if (itemsToDisplay.Any() && listView != null) {
                    listView.FirstItemIndex = 0;
                    listView.LastItemIndex = itemsToDisplay.Count - 1;

                    foreach (var item in itemsToDisplay) {
                        var itemsListElementUI = new ItemsListElementUI(item, index++, GetViewParentForItem(item));
                        InitListElement(itemsListElementUI);
                    }
                }
            }
            
            RefreshItemsCount();
            ItemsByTab.Values.ForEach(list => list.listView.Refresh());
            
            if (ParentModel.Config.TryToFocusFirstItemOnTheList) {
                DelayedFocus().Forget();
            }

            _filterOverride = null;
            _sortingOverride = null;
        }
        
        public void RefreshSoftly() {
            if (HasBeenDiscarded || ItemsByTab.IsEmpty()) {
                return;
            }

            if (IsMultipleLists) {
                RefreshListsAndRemoveEmptyTabs();
            }
            RefreshOrder();
        }
        
        Transform GetViewParentForItem(Item item) {
            Transform viewParent = ItemsByTab.Where(valuePair => valuePair.Value.items.Contains(item))
                .Select(i => i.Value.listView.ItemHost)
                .FirstOrDefault();
            
            if (viewParent == null) {
                viewParent = ItemsByTab[ItemsTabType.None].listView.ItemHost;
            }

            return viewParent;
        }

        void RefreshItemsCount() {
            if (!_onlyOneList) {
                foreach (var list in ItemsByTab) {
                    var listView = list.Value.listView;
                    listView.ItemsCount = list.Value.items.Count;
                    
                    if (IsMultipleLists) {
                        listView.RefreshSelfState();
                    }
                }
                
                if (_viewHostWithCategory != null && IsEmpty) {
                    _viewHostWithCategory.SortView.TrySetActiveOptimized(false);
                }
                
                RefreshGridSize();
            }
        }

        void RefreshGridSize() {
            int allRowsCount = 0;
            int tabCount = ItemsByTab.Count;
            int minRowsCount = ParentModel.Config.MinRowsCount;
            var values = ItemsByTab.Values;
            
            for (int i = 0; i < tabCount; i++) {
                var list = values.ElementAt(i);

                if (i == 0) {
                    ColumnCount = list.listView.DisplayColumnCount;
                }
                    
                if (list.listView.ItemsCount != null) {
                    int rowsCount = (int)math.ceil((float)list.listView.ItemsCount / ColumnCount);
                    allRowsCount += rowsCount;

                    if (i == tabCount - 1 && minRowsCount - allRowsCount > 0) {
                        rowsCount += minRowsCount - allRowsCount;
                    } 
                        
                    list.listView.ChangeGrid(rowCount: rowsCount);
                }
            }
        }
        
        void RefreshOrder() {
            if (HasBeenDiscarded || ItemsByTab.IsEmpty()) {
                return;
            }
            
            ItemsByTab.Values.ForEach(list => {
                list.listView.FirstItemIndex = null;
                list.listView.LastItemIndex = null;
            });
            
            int newIndex = 0;
            
            if (IsMultipleLists) {
                var items = ItemsListElements
                    .GetManagedEnumerator()
                    .GroupBy(element => ItemsByTab.FirstOrDefault(pair => pair.Value.items.Contains(element.Item)).Key)
                    .OrderBy(group => _subTabsInOrder.IndexOf(group.Key))
                    .SelectMany(group => group.OrderBy(element => element.Item, CurrentSorting).Select(element => new { Element = element, CurrentType = group.Key }));
                foreach (var item in items) {
                    ItemsByTab[item.CurrentType].listView.FirstItemIndex ??= newIndex;
                    ItemsByTab[item.CurrentType].listView.LastItemIndex = newIndex;
                    item.Element.RefreshIndex(newIndex++);
                }
            } else {
                var items = ItemsListElements
                    .GetManagedEnumerator()
                    .OrderBy(element => element.Item, CurrentSorting);
                foreach (var item in items) {
                    item.RefreshIndex(newIndex++);
                }
                
                if (ItemsByTab.Count > 0) {
                    var listView = ItemsByTab.FirstOrDefault().Value.listView;
                    if (listView != null) {
                        listView.FirstItemIndex = 0;
                        listView.LastItemIndex = newIndex - 1;
                    }
                } else {
                    Log.Important?.Error("Items in tab are empty. this will result in player not having any items in the list");
                }
            }
            
            RefreshItemsCount();
            ItemsByTab.Values.ForEach(list => list.listView.OrderChanged());
            
            if (FocusGamepadAtElementAtIndex(CurrentIndex) == false) {
                FocusGamepadAtElementAtIndex(CurrentIndex - 1);
            }
        }
        
        void InitListElement(ItemsListElementUI itemsListElementUI) {
            AddElement(itemsListElementUI);
            itemsListElementUI.Item.ListenTo(Item.Events.ActionPerformed, _ => RefreshOnlyElementLayout(itemsListElementUI), this);
        }
        
        void RefreshListsAndRemoveEmptyTabs() {
            List<ItemsTabType> tabsToRemove = new();
            var keys = new ItemsTabType[ItemsByTab.Count];
            ItemsByTab.Keys.CopyTo(keys, 0);

            for (int i = 0; i < keys.Length; i++) {
                var key = keys[i];
                var currentTab = ItemsByTab[key];
                var oldItems = currentTab.items;
                var filteredItems = new List<Item>();
                
                for (int j = 0; j < oldItems.Count; j++) {
                    var item = oldItems[j];
                    
                    if (GetItemsListElementWithItem(item)?.HasBeenDiscarded == false) {
                        filteredItems.Add(item);
                    }
                }

                ItemsByTab[key] = (currentTab.listView, filteredItems);

                if (filteredItems.Count == 0) {
                    (currentTab.listView as View)?.Discard();
                    tabsToRemove.Add(key);
                }
            }

            for (int i = 0; i < tabsToRemove.Count; i++) {
                ItemsByTab.Remove(tabsToRemove[i]);
            }
        }
        
        void RefreshOnlyElementLayout(ItemsListElementUI itemsListElementUI) {
            itemsListElementUI.TriggerChange();
            foreach (var element in Elements<ItemsListElementUI>().GetManagedEnumerator()) {
                if (element == itemsListElementUI || element.View<VItemsListElement>() == null) continue;
                element.TriggerChange();
            }
        }
        
        public void ForceClick(ItemsListElementUI itemsListElement) {
            itemsListElement?.View<VItemsListElement>()?.ForceClick();
        }
        
        public void OverrideFilter(ItemsTabType filterTabType) {
            _filterOverride = filterTabType;
        }
        
        public void OverrideSorting(ItemsSorting sorting) {
            _sortingOverride = sorting;
        }

        public void Filter(ItemsTabType filterTabType) {
            ItemsUI.SetCurrentTabFilter(TabType, filterTabType);
            Refresh();
        }
        
        public void Sort(ItemsSorting sorting) {
            HoveredItemsListElement?.View<VItemsListElement>()?.ForceRefresh();
            ItemsUI.SetCurrentSorting(sorting);
            RefreshOrder();
        }
        
        public async UniTaskVoid DelayedFocus() {
            // 3 frames delay to ensure VCListAdjuster executed two one-frame delays and the grid size is set and items are initialized
            if (await AsyncUtil.DelayFrame(this, 3)){
                TryReceiveFocus();
            }
        }
        
        public override bool TryReceiveFocus() => FocusGamepadAtElementAtIndex(0);

        public bool FocusGamepadAtElementAtIndex(int index) {
            var elements = Elements<ItemsListElementUI>();
            if (index < 0 || elements.CountLessOrEqualTo(index)) {
                return false;
            }
            
            var element = elements.First(e => e.Index == index);
            World.Only<Focus>().Select(element.View<VItemsListElement>());
            return true;
        }
        
        public UIResult HandleEventFor(Item item, UIEvent evt) => ItemsUI.HandleEventFor(item, evt);

        public void OnSelected(Item item) {
            if (!ParentModel.Config.AllowMultipleClickEventsOnTheSameItem && ClickedItem == item) {
                ParentModel.Trigger(ItemsUI.Events.ClickedItemTriggered, item);
                return;
            }
            
            ClickedItem = item;
            CurrentIndex = ClickedItemsListElement?.Index ?? 0;
            ParentModel.Trigger(ItemsUI.Events.ClickedItemsChanged, item);
            RewiredHelper.VibrateUIHover(VibrationStrength.VeryLow, VibrationDuration.VeryShort);
        }

        public void OnDeselected(Item item) {
            if (ClickedItem == item) {
                ClickedItem = null;
            }
        }

        public void OnHoverStarted(Item item) {
            HoveredItem = item;
            CurrentIndex = HoveredItemsListElement?.Index ?? 0;
            ParentModel.Trigger(ItemsUI.Events.HoveredItemsChanged, item);
            RewiredHelper.VibrateUIHover(VibrationStrength.VeryLow, VibrationDuration.VeryShort);
        }

        public void OnHoverEnded(Item item) {
            if (HoveredItem == item) {
                HoveredItem = null;
                ParentModel.Trigger(ItemsUI.Events.HoveredItemsChanged, null);
            }
        }
        
        public void HandelNavigation(UINaviAction naviAction, ItemsListElementUI itemElement) {
            int currentIndex = itemElement.Index;
            NaviDirection direction = naviAction.direction;
            
            if (direction.IsVerticalMovement()) {
                if (IsMultipleLists) {
                    HandleMultipleListNavigation(direction, currentIndex);
                } else {
                    HandleSingleListNavigation(direction, currentIndex);
                }
            } else if (direction == NaviDirection.Left) {
                FocusGamepadAtElementAtIndex(currentIndex - 1);
            } else if (direction == NaviDirection.Right) {
                if (IsLastInRow(currentIndex) && itemElement.NextFocusTarget != null) {
                    World.Only<Focus>().Select(itemElement.NextFocusTarget());
                } else {
                    FocusGamepadAtElementAtIndex(currentIndex + 1);
                }
            }
        }

        void HandleMultipleListNavigation(NaviDirection direction, int currentIndex) {
            if (direction == NaviDirection.Up) {
                if (IsInFirstRow(currentIndex, out IItemsList itemList)) {
                    int prevIndex = CalculatePrevIndex(currentIndex, itemList);
                    FocusGamepadAtElementAtIndex(prevIndex);
                } else {
                    FocusGamepadAtElementAtIndex(currentIndex - ColumnCount);
                }
            } else if (direction == NaviDirection.Down) {
                if (IsInLastRow(currentIndex, out IItemsList itemList, out int lastRowStartIndex)) {
                    int nextIndex = CalculateNextIndex(currentIndex, itemList, lastRowStartIndex);
                    FocusGamepadAtElementAtIndex(nextIndex);
                } else {
                    var maxIndex = (int)Elements<ItemsListElementUI>().Count() - 1;
                    FocusGamepadAtElementAtIndex(Mathf.Clamp(currentIndex + ColumnCount, 0, maxIndex));
                }
            }
        }
        
        void HandleSingleListNavigation(NaviDirection direction, int currentIndex) {
            if (direction == NaviDirection.Up) {
                FocusGamepadAtElementAtIndex(currentIndex - ColumnCount);
            } else if (direction == NaviDirection.Down) {
                if (!IsInLastRow(currentIndex, out _, out _)) {
                    var maxIndex = (int)Elements<ItemsListElementUI>().Count() - 1;
                    FocusGamepadAtElementAtIndex(Mathf.Clamp(currentIndex + ColumnCount, 0, maxIndex));
                }
            }
        }

        bool IsInFirstRow(int index, out IItemsList itemList) {
            if (TryGetItemsList(index, out itemList) == false){ 
                return false;
            }

            int firstItemIndex = itemList.FirstItemIndex ?? 0;
            return index < firstItemIndex + ColumnCount;
        }
        
        int CalculatePrevIndex(int index, IItemsList itemList) {
            int firstItemIndex = itemList.FirstItemIndex ?? 0;
            int indexOrderInRow = index - firstItemIndex;

            var prevList = GetItemsList(firstItemIndex - 1);
            int prevIndex = CalculateLastRowStartIndex(prevList);

            return prevList == itemList || prevList == null ? index : math.min(prevIndex + indexOrderInRow, prevList.LastItemIndex ?? index);
        }
        
        bool IsInLastRow(int index, out IItemsList itemList, out int lastRowStartIndex) {
            if (TryGetItemsList(index, out itemList) == false){ 
                lastRowStartIndex = -1;
                return false;
            }
            
            lastRowStartIndex = CalculateLastRowStartIndex(itemList);
            return index >= lastRowStartIndex;
        }
        
        int CalculateNextIndex(int index, IItemsList itemList, int lastRowStartIndex) {
            int lastItemIndex = itemList.LastItemIndex ?? 0;
            int indexOrderInRow = index - lastRowStartIndex;

            int nextIndex = lastItemIndex + 1;
            var nextList = GetItemsList(nextIndex);

            return nextList == itemList || nextList == null ? index : math.min(nextIndex + indexOrderInRow, nextList.LastItemIndex ?? index);
        }
        
        bool IsLastInRow(int index) {
            var maxIndex = (int)Elements<ItemsListElementUI>().Count() - 1;
            return (index + 1) % ColumnCount == 0 || index == maxIndex;
        }
        
        int CalculateLastRowStartIndex(IItemsList itemList) {
            int fillRowsCount = CalculateFillRowsCount(itemList);
            return (fillRowsCount * ColumnCount) + itemList?.FirstItemIndex ?? (fillRowsCount - 1) * ColumnCount;
        }
        
        int CalculateFillRowsCount(IItemsList itemList) {
            return (itemList?.LastItemIndex - itemList?.FirstItemIndex) / ColumnCount ??
                   Mathf.CeilToInt(Elements<ItemsListElementUI>().Count() / (float)ColumnCount);
        }

        bool TryGetItemsList(int itemIndex, out IItemsList itemsList) {
            itemsList = GetItemsList(itemIndex);

            if (itemsList == null) {
                Log.Important?.Error($"No list found for index: {itemIndex} in {nameof(ItemsListUI)} with id {ParentModel.ID}");
                return false;
            }

            return true;
        }
        
        IItemsList GetItemsList(int itemIndex) {
            return ItemsByTab.Values.FirstOrDefault(list => itemIndex >= list.listView.FirstItemIndex && itemIndex <= list.listView.LastItemIndex).listView;
        }

        public ItemsListElementUI GetItemsListElementWithItem(Item item) {
            return ItemsListElements.FirstOrDefault(el => el.Item == item);
        }
    }
}