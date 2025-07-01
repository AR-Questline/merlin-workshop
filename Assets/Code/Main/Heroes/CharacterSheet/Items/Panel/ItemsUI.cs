using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Memories;
using Awaken.TG.Main.UI.Components.Tabs;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI;
using Awaken.TG.MVC.UI.Events;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel {
    public partial class ItemsUI : Element, ItemsTabs.ITabParent {
        const string SortingKey = "items_sorting";

        public sealed override bool IsNotSaved => true;

        ItemsListUI ItemsListUI => TryGetElement<ItemsListUI>();
        VItemsUI VItemsUI { get; set; }

        string MainMemoryContext => string.IsNullOrEmpty(Config.CustomMemoryContext)
            ? GenericParentModel.GetType().ToString()
            : Config.CustomMemoryContext;

        string EquipmentCategory => Config.EquipmentSlotType != null
            ? Config.EquipmentSlotType.EquipmentCategory.ToString()
            : string.Empty;

        public IItemsUIConfig Config { get; }
        public ItemsTabType CurrentType { get; set; }
        public Tabs<ItemsUI, VItemsTabs, ItemsTabType, ItemsListUI> TabsController { get; set; }
        public Transform TabButtonsHost { get; private set; }
        public Transform ContentHost { get; private set; }

        public IEnumerable<Item> Items => Config.Items;
        public IEnumerable<ItemsTabType> Tabs => Config.Tabs;
        public ItemDescriptorType ItemDescriptorType => Config.ItemDescriptorType;
        public Item HoveredItem => ItemsListUI?.HoveredItem;
        public Item ClickedItem => ItemsListUI?.ClickedItem;
        [UnityEngine.Scripting.Preserve] public ItemsListElementUI HoveredItemsListElement => ItemsListUI.HoveredItemsListElement;
        public ItemsListElementUI ClickedItemsListElement => ItemsListUI.ClickedItemsListElement;
        public Dictionary<ItemsTabType, ItemsTabType> CurrentFiltersMap { get; }

        public new static class Events {
            public static readonly Event<ItemsUI, Item> ClickedItemsChanged = new(nameof(ClickedItemsChanged));
            // Useful for actions that need to be triggered when an item is already clicked e.g. moving gamepad focus from item list to ingredients
            public static readonly Event<ItemsUI, Item> ClickedItemTriggered = new(nameof(ClickedItemTriggered));
            public static readonly Event<ItemsUI, Item> HoveredItemsChanged = new(nameof(HoveredItemsChanged));
            public static readonly Event<ItemsUI, IEnumerable<Item>> ItemsCollectionChanged = new(nameof(ItemsCollectionChanged));
        }
        
        public ItemsUI(IItemsUIConfig config) {
            Config = config;
            CurrentType = Tabs.First();
            CurrentFiltersMap = Config.EquipmentSlotType?.FilterTabs.ToDictionary(mainTab => mainTab, _ => ItemsTabType.None) 
                                ?? Tabs.ToDictionary(mainTab => mainTab, _ => ItemsTabType.None);
        }

        protected override void OnFullyInitialized() {
            this.ListenTo(Events.ItemsCollectionChanged, RefreshUI);
            Hero.Current.Inventory.ListenTo(ICharacterInventory.Events.PickedUpNewItem, SoftRefresh, this);

            if (World.SpawnView(this, Config.ItemsUIView, false, true, Config.ItemsHost) is VItemsUI vItemsUI) {
                VItemsUI = vItemsUI;
                TabButtonsHost = VItemsUI.TabButtonsHost;
                ContentHost = VItemsUI.ContentHost;
            }
            
            AddElement(new ItemsTabs());
        }

        public void SetupTitle() {
            if (VItemsUI is IItemsListTitle itemsGridTitle) {
                itemsGridTitle.SetupTitle(CurrentType.Title, Config.ContextTitle);
            }
        }
        
        public ItemsSorting GetCurrentSorting(ItemsTabType tabType) {
            var defaultSorting = ItemsSorting.DefaultSorting(tabType).ToString();
            var currentSortingEnum = Services.Get<GameplayMemory>()
                .Context(MainMemoryContext, EquipmentCategory)
                .Get(SortingKey, defaultSorting);
            return RichEnumReference.GetEnum(currentSortingEnum) as ItemsSorting;
        }

        public void SetCurrentSorting(ItemsSorting sorting) {
            Services.Get<GameplayMemory>()
                .Context(MainMemoryContext, EquipmentCategory)
                .Set(SortingKey, sorting.ToString());
        }

        public ItemsTabType GetCurrentTabFilter(ItemsTabType tabType) {
            return tabType.ContainsSubTab(CurrentFiltersMap[tabType]) ? CurrentFiltersMap[tabType] : ItemsTabType.None;
        }
        
        public void SetCurrentTabFilter(ItemsTabType tabType, ItemsTabType filterTabType) {
            if(tabType.ContainsSubTab(filterTabType) == false) { 
                Log.Important?.Error($"Trying to set invalid filter {filterTabType.EnumName} for tab {tabType.EnumName}. Ignoring.");
                return;
            }
            
            CurrentFiltersMap[tabType] = filterTabType;
        }
        
        public UIResult HandleEventFor(Item item, UIEvent evt) => Config.HandleItemEvent(item, evt);

        public ItemsListElementUI GetItemsListElementWithItem(Item item) {
            return ItemsListUI.GetItemsListElementWithItem(item);
        }

        public void ForceClick(Item item) {
            ItemsListUI.ForceClick(GetItemsListElementWithItem(item));
        }
        
        public async UniTaskVoid FocusFirstItem() {
            if (await AsyncUtil.DelayFrame(ItemsListUI)) {
                ItemsListUI.FocusGamepadAtElementAtIndex(0);
            }
        }

        void RefreshUI(IEnumerable<Item> items) {
            bool any = Items.Any(CurrentType.Contains);
            
            if (!any) { 
                Element<ItemsTabs>().Refresh();
            } else {
                if (CurrentType == ItemsTabType.All || CurrentType == ItemsTabType.AllWithRecent) {
                    Element<ItemsTabs>().Refresh();
                }
                
                SoftRefresh();
            }
        }
        
        /// <summary>
        /// Update item list indices
        /// </summary>
        public void SoftRefresh() {
            ItemsListUI.RefreshSoftly();
        }
        
        /// <summary>
        /// Regenerate item list
        /// </summary>
        public void FullRefresh() {
            RegenerateItemsList().Forget();
        }

        async UniTaskVoid RegenerateItemsList() {
            ItemsListUI.Refresh();
            
            if (await AsyncUtil.DelayFrame(ItemsListUI)) {
                ItemsListUI.FocusGamepadAtElementAtIndex(0);
            }
        }
    }
}