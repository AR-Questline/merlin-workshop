using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Shops.UI;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.LeftScreen.SpecialItem;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Shops.Tabs {
    public abstract partial class ShopVendorBaseUI : ShopUITab<VShopVendorBaseUI>, IItemsUIConfig {
        Model _popup;
        Prompt _vendorPrompt;
        InputItemQuantityUI _inputItemQuantityUI;

        ShopUI ShopUI => ParentModel;
        ItemsUI ItemsUI { get; set; }
        Item HoveredItem => ItemsUI.HoveredItem;
        Transform IItemsUIConfig.ItemsHost => View.ItemsHost;
        string CantAffordInfo => LocTerms.UIShopCantAfford.Translate();
        VShopTooltipSystemUI Tooltip => Element<ItemTooltipUI>().View<VShopTooltipSystemUI>();
        
        protected static Hero Hero => Hero.Current;
        protected abstract string TradeActionName { get; }
        protected VShopVendorBaseUI View => View<VShopVendorBaseUI>();
        protected Shop Shop => ParentModel.ParentModel;
        
        public abstract IMerchant Seller { get; }
        public abstract IMerchant Buyer { get; }
        public abstract IEnumerable<Item> Items { get; }
        
        public string ContextTitle => LocTerms.UIItemsShop.Translate();
        public IEnumerable<ItemsTabType> Tabs => ItemsTabType.Shop;
        public Prompts Prompts => ShopUI.Prompts;
        public ItemDescriptorType ItemDescriptorType => ItemDescriptorType.VendorItem;
        public string CustomMemoryContext => "ShopVendorBaseUI";
        public bool UseCategoryList => true;
        bool IsEmpty => !Items.Any();

        ItemTooltipUI _tooltip;

        protected override void AfterViewSpawned(VShopVendorBaseUI view) {
            _tooltip = AddElement(new ItemTooltipUI(typeof(VShopTooltipSystemUI), ParentModel.TooltipParent, 0f, 0f, 0f, true, preventDisappearing: true));
            ShowEmptyInfo();

            ItemsUI = AddElement(new ItemsUI(this));
            ItemsUI.ListenTo(ItemsUI.Events.HoveredItemsChanged, OnHoveredItemChanged, this);
            ItemsUI.ListenTo(ItemsUI.Events.ClickedItemsChanged, Trade, this);
            ItemsUI.ListenTo(ItemsUI.Events.ItemsCollectionChanged, ShowEmptyInfo, this);
            
            _vendorPrompt = Prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, TradeActionName), this, false);
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = true;
            World.Only<SpecialItemNotificationBuffer>().SuspendPushingNotifications = true;
        }
        
        void ShowEmptyInfo() {
            SetupEmptyInfoLabels();
            
            if (IsEmpty) {
                _tooltip.ForceDisappear();
            }
            
            this.Trigger(IEmptyInfo.Events.OnEmptyStateChanged, !IsEmpty);
        }

        protected virtual void SetupEmptyInfoLabels() { }
        
        public int AffordableItemsAmount() {
            var tradeItem = HoveredItem;
            return TradeUtils.AffordableItemsAmount(Seller, Buyer, tradeItem);
        }
        
        protected virtual void OnSuccessfulTrade() { }

        protected virtual void PlayCantAffordSfx() {
            View.PlayCantAffordSfx();
        }

        void OnHoveredItemChanged(Item item) {
            _vendorPrompt.SetActive(item != null);
            CheckIfCanAfford(item);
        }

        void Trade() {
            if (HoveredItem is {Quantity: > 1} && AffordableItemsAmount() >= 1) {
                _inputItemQuantityUI = new InputItemQuantityUI(HoveredItem);
                AddElement(_inputItemQuantityUI);
                var popupContent = new DynamicContent(_inputItemQuantityUI, typeof(VInputShopItemQuantityUI));

                var tradeItem = HoveredItem;
                _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                    string.Empty,
                    PopupUI.AcceptTapPrompt(() => AcceptTrade(tradeItem)),
                    PopupUI.CancelTapPrompt(ClosePopup),
                    LocTerms.Quantity.Translate(),
                    popupContent
                );
            } else {
                TryTrade(HoveredItem);
            }
        }

        void AcceptTrade(Item item) {
            if (TryTrade(item, _inputItemQuantityUI.Value)) {
                ClosePopup();
            }
        }

        bool TryTrade(Item tradeItem, int itemQuantity = 1) {
            if (tradeItem.IsStolen && Buyer is Shop { Template: { IsFence: false } }) {
                PlayCantAffordSfx();
                return false;
            }

            if (!TradeUtils.TryTrade(Seller, Buyer, tradeItem, itemQuantity)) {
                PlayCantAffordSfx();
                return false;
            }
            
            OnSuccessfulTrade();
            if ((tradeItem?.HasBeenDiscarded ?? true) || tradeItem.Inventory != Seller.Inventory) {
                // Seller no longer owns this item
                ItemsUI.GetItemsListElementWithItem(tradeItem)?.Discard();
                ItemsUI.Trigger(ItemsUI.Events.ItemsCollectionChanged, Items);
            }
            
            return true;
        }

        void CheckIfCanAfford(Item item) {
            if (item == null) {
                return;
            }
            
            Tooltip.SetCantAffordText(AffordableItemsAmount() < 1 ? CantAffordInfo : string.Empty);
        }

        void ClosePopup() {
            _inputItemQuantityUI?.Discard();
            _inputItemQuantityUI = null;
            _popup?.Discard();
            _popup = null;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = false;
            World.Only<SpecialItemNotificationBuffer>().SuspendPushingNotifications = false;
        }
    }
}