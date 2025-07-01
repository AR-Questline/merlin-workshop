using System;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.CharacterSheet.Inventory;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Choose;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment {
    public partial class EquipmentChooseUI : ItemChooseUI<LoadoutsUI>, IClosable {
        readonly IEquipmentSlot _targetSlot;
        Prompt _promptUnselect;
        Prompt _promptDrop;

        CharacterSheetUI CharacterSheetUI => ParentModel.CharacterSheetUI;
        InventoryUI InventoryUI => ParentModel.InventoryUI;
        
        public EquipmentSlotType EquipmentSlotType => _targetSlot.Type;

        public override Type ItemsListElementView => typeof(VItemEqChooseElement);
        public override Type ItemsCategoryListHostView => typeof(VHostItemsListWithCategoryEquipment);
        public override ItemsTabType SortingTab => EquipmentSlotType.SortingTab;
        public override bool UseCategoryList => true;
        
        public EquipmentChooseUI(IEquipmentSlot targetSlot) : base(targetSlot.Type.FilterTabs) {
            _targetSlot = targetSlot;
        }

        protected override void OnFullyInitialized() {
            base.OnFullyInitialized();
            ParentModel.HeroItems.Owner.ListenTo(IItemOwner.Relations.Owns.Events.Changed, _ => Element<ItemsUI>().TriggerChange(), this);
            _itemsUI.ListenTo(ItemsUI.Events.ItemsCollectionChanged, OnItemDrop, this);
            CharacterSheetUI.SetRendererTarget(EquipmentSlotType);
            CharacterSheetUI.HeroRenderer.SetRotatableState(false);

            SetupEmptyInfo();
        }

        protected override void AddPrompts() {
            base.AddPrompts();
            _promptSelect.ChangeName(LocTerms.UIItemsEquip.Translate());
            _promptUnselect = Prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Items.UnequipItem, LocTerms.UIItemsUnequip.Translate(), UnequipItem), this, false);
            _promptDrop = Prompts.AddPrompt(Prompt.Tap(
                KeyBindings.UI.Items.DropItem, LocTerms.UIItemsDropItem.Translate(), () => InventoryUI.DropCurrentItem(Element<ItemsUI>().HoveredItem, Element<ItemsUI>())), this, false);
        }

        protected override void HoveredItemsChanged(Item item) {
            var hoverItemSlot = ParentModel.HeroItems.SlotWith(item);
            _promptSelect.SetActive(item != null && (item.StatsRequirements is { RequirementsMet: true } || item.IsEquippable) &&
                                    (!item.IsEquipped || IsEquippedInOtherSlot(item, hoverItemSlot)));
            _promptUnselect.SetActive(item is { IsEquipped: true });
            _promptDrop.SetActive(item is { CannotBeDropped: false });

            World.Any<ItemTooltipUI>()?.SetDescriptor(item != null ? new ExistingItemDescriptor(item) : null);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            CharacterSheetUI.SetRendererTarget(HeroRenderer.Target.HeroUIInventory);
            ParentModel.FadeLoadouts(1f);
        }

        protected override void Choose(Item item) {
            _targetSlot.Equip(item);
            HoveredItemsChanged(item);
            ParentModel.RefreshLoadoutNewThings();
        }

        protected override void AfterChoose() {
            //close choose panel after selecting consumable
            if (EquipmentSlotType.SortingTab == ItemsTabType.EquippableConsumable) {
                Discard();
            } else {
                //refresh items list after selecting item to update equipped state
                _itemsUI.SoftRefresh();
            }
        }

        protected override bool ItemFilter(Item item) {
            HeroLoadout loadout = _targetSlot is VCLoadoutSlot vcLoadout ? vcLoadout.Loadout : null;
            return EquipmentSlotType.Accept(item, loadout);
        }

        protected override void SelectCurrent() {
            if (_itemsUI.HoveredItem is { Locked: true }) {
                return;
            }
            
            var hoveredItem = _itemsUI.HoveredItem;
            var hoverItemSlot = ParentModel.HeroItems.SlotWith(hoveredItem);
            var itemInSlot = _targetSlot.ItemInSlot;
            
            if (IsEquippedInOtherSlot(hoveredItem, hoverItemSlot) && hoveredItem != itemInSlot) {
                if (itemInSlot != null && _targetSlot is VCLoadoutSlot loadoutSlot) {
                    SwapItemsInLoadouts(hoveredItem, hoverItemSlot, loadoutSlot.Loadout);
                    return;
                }
                
                UnequipItem(hoveredItem);
            }
            
            if (_itemsUI.HoveredItem is { IsEquipped: true }) {
                return;
            }
            
            base.SelectCurrent();
        }
        
        bool IsEquippedInOtherSlot(Item item, EquipmentSlotType slotType) {
            return item.IsEquipped && slotType != EquipmentSlotType;
        }

        void SwapItemsInLoadouts(Item item, EquipmentSlotType slotType, HeroLoadout loadout) {
            Hero.Current.Inventory.Equip(_targetSlot.ItemInSlot, slotType, loadout);
            Choose(item);
        }

        void OnItemDrop() {
            if (IsEmpty) {
                SetupEmptyInfo();
            }
        }

        void SetupEmptyInfo() {
            bool isArmorOrAccessory = EquipmentSlotType.Armors.Contains(EquipmentSlotType) || EquipmentSlotType.Accessories.Contains(EquipmentSlotType);
            bool isWeapon = EquipmentSlotType.Loadouts.Contains(EquipmentSlotType);
            bool isQuickSlot = EquipmentSlotType.QuickSlots.Contains(EquipmentSlotType) || EquipmentSlotType.ManualQuickSlots.Contains(EquipmentSlotType);

            string description = isArmorOrAccessory ? LocTerms.EqNoArmorAndJewelery.Translate()
                : isWeapon ? LocTerms.EqNoWeapon.Translate()
                : isQuickSlot ? LocTerms.EqNoFoodAndPotion.Translate()
                : string.Empty;

            if (isArmorOrAccessory) {
                ParentModel.SetArmorContentActive(!IsEmpty, description);
            } else if (isWeapon || isQuickSlot) {
                ParentModel.SetWeaponOrQuickSlotContentActive(!IsEmpty, description);
            }
        }

        void UnequipItem() {
            UnequipItem(_itemsUI.HoveredItem);
        }

        void UnequipItem(Item item) {
            if (item == null) {
                Discard();
                return;
            }

            if (item.Locked) {
                return;
            }

            if (item.Inventory is HeroItems heroItems) {
                heroItems.Unequip(item);
                HoveredItemsChanged(item);
            }

            _itemsUI.Trigger(ItemsUI.Events.ItemsCollectionChanged, Items);
        }

        public void Close() => Discard();

        protected override void OnBeforeDiscard() {
            CharacterSheetUI.HeroRenderer.SetRotatableState(true);
            base.OnBeforeDiscard();
        }
    }
}