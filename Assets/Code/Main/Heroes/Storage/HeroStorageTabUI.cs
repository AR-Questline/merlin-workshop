using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Storage {
    public abstract partial class HeroStorageTabUI : HeroStorageTabs.Tab<VHeroStorageTabUI>, IItemsUIConfig {
        Prompt _promptSelectItem;
        PopupUI _popup;
        InputItemQuantityUI _inputItemQuantityUI;

        protected abstract string ActionName { get; }
        protected abstract IInventory InventoryFrom { get; }
        protected abstract IInventory InventoryTo { get; }
        
        VHeroStorageTabUI View => View<VHeroStorageTabUI>();
        HeroStorageUI StorageUI => ParentModel;
        protected static HeroItems HeroItems => Hero.Current.HeroItems;
        protected HeroStorage Storage => StorageUI.Storage;
        
        ItemsUI ItemsUI => Element<ItemsUI>();
        Item HoveredItem => ItemsUI.HoveredItem;
        Transform IItemsUIConfig.ItemsHost => View.ItemsHost;
        public abstract IEnumerable<Item> Items { get; }

        public IEnumerable<ItemsTabType> Tabs => ItemsTabType.Storage;
        public Prompts Prompts => StorageUI.Prompts;
        public ItemDescriptorType ItemDescriptorType => ItemDescriptorType.ExistingItem;
        public bool UseCategoryList => true;
        bool IsEmpty => !Items.Any();

        protected override void AfterViewSpawned(VHeroStorageTabUI view) {
            AddElement(new ItemsUI(this));
            
            OnContentChange();
            ItemsUI.ListenTo(ItemsUI.Events.HoveredItemsChanged, OnHoveredItemChanged, this);
            ItemsUI.ListenTo(ItemsUI.Events.ClickedItemsChanged, SelectItem, this);
            ItemsUI.ListenTo(ItemsUI.Events.ItemsCollectionChanged, OnContentChange, this);

            _promptSelectItem = Prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, ActionName), this, false);
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = true;
        }
        
        void OnContentChange() {
            if (IsEmpty) {
                ParentModel.ForceTooltipDisappear();
            }

            this.Trigger(IEmptyInfo.Events.OnEmptyStateChanged, !IsEmpty);
        }

        void OnHoveredItemChanged(Item item) {
            _promptSelectItem.SetActive(item != null);
        }

        void SelectItem() {
            if (HoveredItem == null) {
                return;
            }
            
            if (HoveredItem.Quantity == 1) {
                SelectItem(HoveredItem, 1);
                return;
            }

            _inputItemQuantityUI = new InputItemQuantityUI(HoveredItem);
            AddElement(_inputItemQuantityUI);
            var popupContent = new DynamicContent(_inputItemQuantityUI, typeof(VInputItemQuantityUI));

            var tradeItem = HoveredItem;
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                string.Empty,
                PopupUI.AcceptTapPrompt(() => {
                    SelectItem(tradeItem, _inputItemQuantityUI.Value);
                    ClosePopup();
                }),
                PopupUI.CancelTapPrompt(ClosePopup),
                LocTerms.Quantity.Translate(),
                popupContent
            );
        }

        void SelectItem(Item item, int quantity) {
            item.MoveTo(InventoryTo, quantity);

            if (SellerNoLongerOwnsItem(item)) {
                ItemsUI.GetItemsListElementWithItem(item)?.Discard();
                ItemsUI.Trigger(ItemsUI.Events.ItemsCollectionChanged, Items);
            }
        }
        
        bool SellerNoLongerOwnsItem(Item item) {
            return item == null || item.HasBeenDiscarded || item.Inventory != InventoryFrom;
        }

        void ClosePopup() {
            _inputItemQuantityUI?.Discard();
            _inputItemQuantityUI = null;
            _popup?.Discard();
            _popup = null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = false;
        }
    }
    
    public partial class HeroStoragePutUI : HeroStorageTabUI {
        protected override string ActionName => LocTerms.UIStoragePut.Translate();
        protected override IInventory InventoryFrom => HeroItems;
        protected override IInventory InventoryTo => Storage;
        public override IEnumerable<Item> Items => HeroItems.StashableInventory;
    }
    
    public partial class HeroStorageTakeUI : HeroStorageTabUI {
        protected override string ActionName => LocTerms.UIStorageTake.Translate();
        protected override IInventory InventoryFrom => Storage;
        protected override IInventory InventoryTo => HeroItems;
        public override IEnumerable<Item> Items => Storage.Items;
    }
}