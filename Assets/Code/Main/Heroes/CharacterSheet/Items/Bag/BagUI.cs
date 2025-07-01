using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Inventory;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Bag {
    public partial class BagUI : InventorySubTab<VBagUI>, IItemsUIConfig {
        public string ContextTitle => LocTerms.UIItemsBag.Translate();
        public IEnumerable<Item> Items => InventoryUI.Items;
        public IEnumerable<ItemsTabType> Tabs => ItemsTabType.Bag;
        public bool UseCategoryList => true;

        Transform IItemsUIConfig.ItemsHost => View<VBagUI>().ItemsHost;
        CharacterSheetUI CharacterSheet => ParentModel.ParentModel;
        Prompts Prompts => CharacterSheet.Prompts;
        InventoryUI InventoryUI => ParentModel;
        bool IsEmpty => !Items.Any();

        Item _hoveredItem;
        Prompt _promptDrop;
        Prompt _promptUse;
        Prompt _promptUnequip;
        ItemTooltipUI _tooltip;

        protected override void AfterViewSpawned(VBagUI view) {
            _tooltip = AddElement(new ItemTooltipUI(typeof(VBagItemTooltipSystemUI), CharacterSheet.StaticTooltip, 0f, 0f, 0f, true, preventDisappearing: true));
            CharacterSheet.SetHeroOnRenderVisible(false);

            OnContentChange();

            var items = AddElement(new ItemsUI(this));
            items.ListenTo(ItemsUI.Events.HoveredItemsChanged, RefreshPrompts, this);
            items.ListenTo(ItemsUI.Events.ClickedItemsChanged, UseItem, this);
            items.ListenTo(ItemsUI.Events.ItemsCollectionChanged, OnContentChange, this);

            _promptUse = Prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.UIItemsUse.Translate()), this, false);
            _promptUnequip = Prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Items.UnequipItem, LocTerms.UIItemsUnequip.Translate(), UnequipItem), this, false);
            _promptDrop = Prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Items.DropItem, LocTerms.UIItemsDropItem.Translate(), () => InventoryUI.DropCurrentItem(_hoveredItem, Element<ItemsUI>())), this, false);

            _promptDrop.AddAudio(new PromptAudio {
                TapSound = view.DropHoldSound
            });
        }

        void OnContentChange() {
            if (IsEmpty) {
                _tooltip.ForceDisappear();
            }

            this.Trigger(IEmptyInfo.Events.OnEmptyStateChanged, !IsEmpty);
        }

        void RefreshPrompts(Item item) {
            _hoveredItem = item;
            _promptDrop.SetActive(item is { CannotBeDropped: false });

            if (item?.UseActionName == null || item.IsThrowable) {
                //TODO handle equipping throwable items for Hero
                _promptUnequip.SetActive(false);
                _promptUse.SetActive(false);
            } else if (item.IsEquippable && !item.IsConsumable && !item.IsEdible) {
                _promptUse.SetActive(item is { IsEquipped: false });
                _promptUse.ChangeName(LocTerms.UIItemsEquip.Translate());
                _promptUnequip.SetActive(item is { IsEquipped: true });
            } else {
                _promptUse.SetActive(true);
                _promptUse.ChangeName(item.UseActionName);
                _promptUnequip.SetActive(false);
            }
        }
        
        void UseItem() {
            if (_hoveredItem is { IsEquipped: false } or { IsEdible: true } or { IsConsumable: true }) {
                PerformItemAction();
            }
        }
        
        void UnequipItem() {
            if (_hoveredItem is { IsEquipped: true }) {
                PerformItemAction();
            }
        }

        void PerformItemAction() {
            if (_hoveredItem is { Locked: false }) {
                _hoveredItem.Use();
                RefreshPrompts(_hoveredItem);
                _tooltip.SetDescriptor(new ExistingItemDescriptor(_hoveredItem));
            }
        }
    }
}