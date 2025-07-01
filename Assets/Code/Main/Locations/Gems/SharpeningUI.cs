using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    public abstract partial class SharpeningUI<T> : GemsBaseUI<T> where T : VGemBaseUI {
        protected Item _upgradedItem;
        
        public override string ContextTitle => LocTerms.SharpenTab.Translate();
        public override IEnumerable<CountedItem> Ingredients => ClickedItem == null ? null : GetIngredientsFromConfig(ItemUpgradeConfigConfig, ClickedItem.Level.ModifiedInt);
        public override int ServiceCost {
            get {
                int serviceCost = base.ServiceCost + (ItemUpgradeConfigConfig?.GetPrice(CurrencyType.Money, ClickedItem.Level.ModifiedInt) ?? 0);
                return (int)((1 - Hero.HeroStats.UpgradeDiscount.ModifiedValue) * serviceCost);
            }
        }
        public override int CobwebServiceCost {
            get {
                int serviceCost = base.CobwebServiceCost + (ItemUpgradeConfigConfig?.GetPrice(CurrencyType.Cobweb, ClickedItem.Level.ModifiedInt) ?? 0);
                return (int)((1 - Hero.HeroStats.UpgradeDiscount.ModifiedValue) * serviceCost);
            
            }
        }
        public override Type ItemsListUIView => typeof(VItemsListSimpleUI);
        protected virtual Type TooltipType => typeof(VSharpenTooltipSystemUI);
        protected override Func<Item, bool> ItemFilter => item => GetSharpeningConfig(item) != null &&
                                                                  !item.HiddenOnUI && item.IsEquippable && item.Template.CanHaveItemLevel
                                                                  && item.Template.EquipmentType != EquipmentType.QuickUse
                                                                  && item.IsGear && !item.IsArrow && !item.IsMagic;
        protected override bool TooltipComparerActive => true;
        protected override string GemActionName => LocTerms.RelicsPromptSharpen.Translate();
        protected override int ServiceBaseCost => Services.Get<GameConstants>().sharpeningBaseCost;
        protected override int CostMultiplier => Mathf.Max(_upgradedItem?.Level.ModifiedInt ?? 1, 1);
        protected virtual ItemUpgradeConfigData ItemUpgradeConfigConfig => ClickedItem != null ? GetSharpeningConfig(ClickedItem) : null;

        VItemsListElement VItemsListElement => ItemsListElementUI is {HasBeenDiscarded: false} ? ItemsListElementUI.View<VItemsListElement>() : null;

        [UnityEngine.Scripting.Preserve]
        public void RefreshFocus() {
            if (VItemsListElement != null) {
                World.Only<Focus>().Select(VItemsListElement);
            }
        }
        
        protected void ReequipIfNecessary() {
            var item = ClickedItem;
            if (item.IsEquipped) {
                var slot = item.EquippedInSlotOfType;
                var inventory = item.CharacterInventory;
                inventory.Unequip(item);
                inventory.Equip(item, slot);
            }
        }

        protected static IEnumerable<CountedItem> GetIngredientsFromConfig(ItemUpgradeConfigData config, int level) {
            IEnumerable<CountedItem> query = Enumerable.Empty<CountedItem>();
            if (config != null) {
                query = config.GetIngredients(Mathf.Abs(level)); //temporary solution to prevent negative level, should be fixed in the future
                query = query.Select(i => new CountedItem(i.itemTemplate, i.quantity * GameConstants.Get.sharpeningHeroIngredientMultiplier));
            }

            return query;
        }

        protected override void OnSelectedItemClickedAgain(Item item) {
            World.Only<Focus>().Select(ItemsListElementUI.NextFocusTarget?.Invoke());
        }

        protected override bool CanRunAction(Item item) {
            return GetSharpeningConfig(item) != null && CheckIngredients();
        }

        protected sealed override void SpawnItemTooltip() {
            _itemTooltipUI = new ItemTooltipUI(
                viewType: TooltipType,
                host: TooltipParentStatic,
                appearDelay: -1f,
                alphaTweenTime: 0.2f,
                isStatic: true,
                comparerActive: TooltipComparerActive);
            
            AddElement(_itemTooltipUI);
        }

        protected sealed override void RefreshItemTooltip() {
            if (ClickedItem is { HasBeenDiscarded: false }) {
                CreateUpgradedItemPreview(ClickedItem);
                ItemTooltipUI.ConstantDescriptorToCompare = new ExistingItemDescriptor(ClickedItem);
                ItemTooltipUI.SetDescriptor(new ExistingItemDescriptor(_upgradedItem));
            } else {
                ItemTooltipUI.SetDescriptor(null);
            }
        }

        protected virtual void CreateUpgradedItemPreview(Item item) {
            _upgradedItem?.Discard();
            _upgradedItem = World.Add(new Item(item.Template, 1, item.Level.ModifiedInt));
            ItemUtils.CopyItemStats(item, _upgradedItem);
            _upgradedItem.Level.IncreaseBy(1);
        }
        
        protected sealed override void OnGamepadSlotSelect(Item item) {
            // override to prevent the default behavior - we don't want to click the item unnecessarily
        }

        protected sealed override void OnTabChanged(ItemsListUI itemsListUI) {
            View.HideRightSide();
        }

        protected virtual ItemUpgradeConfigData GetSharpeningConfig(Item item) {
            return item?.Template.ItemUpgradeConfigConfig;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            _upgradedItem?.Discard();
            _upgradedItem = null;
        }

        public struct SharpeningChangeData {
            public Item item;
            public int newLevel;
            public IEnumerable<CountedItem> itemsUsed;
            
            public SharpeningChangeData(Item item, int newLevel, IEnumerable<CountedItem> itemsUsed) {
                this.item = item;
                this.newLevel = newLevel;
                this.itemsUsed = itemsUsed;
            }
        }
    }

    public partial class SharpeningUI : SharpeningUI<VSharpeningUI> {
        PopupUI _popup;
        
        protected override void GemAction() {
            if (!ClickedItem) {
                return;
            }
            
            PayForService();
            
            ClickedItem.Level.IncreaseBy(1);
            ClickedItem.Trigger(Item.Events.ItemSharpened, new SharpeningChangeData(
                ClickedItem, ClickedItem.Level.ModifiedInt, 
                GetIngredientsFromConfig(ItemUpgradeConfigConfig, ClickedItem.Level.ModifiedInt - 1))
            );
            
            ReequipIfNecessary();
            Refresh();
        }
    }
}