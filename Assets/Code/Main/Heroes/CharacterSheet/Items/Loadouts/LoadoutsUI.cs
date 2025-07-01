using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet.Inventory;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Choose;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Equipment;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.CharacterSheet.Items.Loadouts {
    public partial class LoadoutsUI : InventorySubTab<VLoadoutsUI>, IItemChooseParent {
        public CurrentlyHovered<IEquipmentSlot> CurrentlyHoveredSlot { get; } = new();

        bool _preventHovering;
        bool _isLoadoutHovered;
        Prompt _promptEquip;
        Prompt _promptUnequip;
        Prompt _promptDrop;
        bool _previousChangeWasFromLoadoutToLoadout;
        
        public HeroItems HeroItems => InventoryUI.HeroItems;
        public Prompts Prompts => CharacterSheetUI.Prompts;
        public IEnumerable<Item> PossibleItems => InventoryUI.ItemsForLoadout;
        public CharacterSheetUI CharacterSheetUI => ParentModel.ParentModel;
        public InventoryUI InventoryUI => ParentModel;
        public VCLoadoutSlot CurrentlyChangedLoadout { get; private set; }
        public Transform ChooseHost { get; private set; }

        VEquipmentUI EquipmentView => View<VEquipmentUI>();
        VLoadoutsUI _loadoutView;

        protected override void AfterViewSpawned(VLoadoutsUI view) {
            _loadoutView = view;
            AddElement(new ItemTooltipUI(typeof(VItemTooltipSystemUI), CharacterSheetUI.OverlayLayer, 0.2f));
            
            CharacterSheetUI.SetRendererTargetInstant(HeroRenderer.Target.HeroUIInventory);
            CharacterSheetUI.SetHeroOnRenderVisible(true);
            _promptEquip = Prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Items.SelectItem, LocTerms.UIItemsEquip.Translate(), Equip), this, false).AddAudio();
            _promptUnequip = Prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Items.UnequipItem, LocTerms.UIItemsUnequip.Translate(), Unequip), this, false);
            _promptDrop = Prompts.AddPrompt(Prompt.Tap(
                KeyBindings.UI.Items.DropItem, LocTerms.UIItemsDropItem.Translate(), Drop), this, false);
            CurrentlyHoveredSlot.OnChange += OnHoveredSlotChange;
            
            View<IEmptyInfo>().PrepareEmptyInfo();
            this.Trigger(IEmptyInfo.Events.OnEmptyStateChanged, true);
        }

        public void SelectLoadout(int index) {
            HeroItems.ActivateLoadout(index);
        }

        public void FadeLoadouts(float targetAlpha) {
            if (HasBeenDiscarded) {
                return;
            }
            
            _loadoutView.FadeLoadouts(targetAlpha);
        }
        
        public void RefreshLoadoutNewThings() {
            _loadoutView.RefreshLoadoutNewThings();
        }
        
        public void SetWeaponOrQuickSlotContentActive(bool active, string description = null) {
            _loadoutView.WeaponEmptyInfo.SetupLabels(LocTerms.EmptyNoItems.Translate(), description);
            _loadoutView.WeaponEmptyInfo.SetContentActive(active);
        }
        
        public void SetArmorContentActive(bool active, string description = null) {
            _loadoutView.EmptyInfoView.SetupLabels(LocTerms.EmptyNoItems.Translate(), description);
            _loadoutView.EmptyInfoView.SetContentActive(active);
        }
        
        public void OnLoadoutHoverChange(bool active) {
            bool shouldEquipBeActive = active || _previousChangeWasFromLoadoutToLoadout;
            _promptEquip.SetActive(shouldEquipBeActive || CurrentlyHoveredSlot.Get != null);
            bool canBeUnequipped = CurrentlyHoveredSlot.Get is { AllowUnequip: true } && (!CurrentlyHoveredSlot.Get.ItemInSlot?.IsFists ?? true);
            _promptUnequip.SetActive(canBeUnequipped);
            _promptDrop.SetActive(canBeUnequipped && CurrentlyHoveredSlot.Get.ItemInSlot is {CannotBeDropped: false});
            _previousChangeWasFromLoadoutToLoadout = _isLoadoutHovered && active;
            _isLoadoutHovered = shouldEquipBeActive;
        }
        
        void OnHoveredSlotChange(Change<IEquipmentSlot> change) {
            bool active = _isLoadoutHovered || (change.to != null && !_preventHovering && !change.to.Locked);
            _promptEquip.SetActive(active);
            bool canBeUnequipped = active && change.to is { AllowUnequip: true } && (!change.to.ItemInSlot?.IsFists ?? true);
            _promptUnequip.SetActive(canBeUnequipped);
            _promptDrop.SetActive(canBeUnequipped && change.to.ItemInSlot is {CannotBeDropped: false});
        }

        void Equip() {
            var targetSlot = CurrentlyHoveredSlot.Get;
            
            if (targetSlot == null) { 
                return;
            }
            
            CurrentlyChangedLoadout = targetSlot as VCLoadoutSlot;
            
            if (CurrentlyChangedLoadout != null) {
                SelectLoadout(CurrentlyChangedLoadout.LoadoutIndex);
            }
            
            bool changedAccessory = EquipmentSlotType.Accessories.Contains(targetSlot.Type);
            bool changedArmor = EquipmentSlotType.Armors.Contains(targetSlot.Type);
            bool changedRightSection = changedArmor || changedAccessory;
            ChooseHost = changedArmor ? _loadoutView.ArmorChooseHost : changedAccessory ? _loadoutView.JewelryChooseHost : _loadoutView.LeftChooseHost;
            
            var choose = AddElement(new EquipmentChooseUI(targetSlot));
            choose.ListenTo(Events.AfterDiscarded, AfterChose, this);
            
            SetChooseItemVisible(true, !changedRightSection, changedAccessory);
            FadeLoadouts(changedAccessory ? 0.4f : 0f);
            _preventHovering = true;
        }

        void Drop() {
            var targetSlot = CurrentlyHoveredSlot.Get;
            Item itemToDrop = targetSlot.ItemInSlot;
            targetSlot.Unequip();
            InventoryUI.DropCurrentItem(itemToDrop, forcePopupWhenNotSingle: true);
            TriggerChange();
        }
        
        void AfterChose() {
            if (!HasBeenDiscarded) {
                SetChooseItemVisible(false);
                _preventHovering = false;
                TriggerChange();
            }

            this.Trigger(IEmptyInfo.Events.OnEmptyStateChanged, true);
            CurrentlyChangedLoadout = null;
        }
        
        void Unequip() {
            CurrentlyHoveredSlot.Get.Unequip();
            TriggerChange();
        }

        void SetChooseItemVisible(bool visible, bool dimArmorSection = false, bool dimQuickSlots = false) {
            _promptEquip.SetVisible(!visible);
            _promptUnequip.SetVisible(!visible);
            _promptDrop.SetVisible(!visible);
            
            EquipmentView.SetVisible(visible, dimArmorSection, dimQuickSlots);
        }
    }
}