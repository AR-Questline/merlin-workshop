using System;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Choose;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Management;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Gems;
using Awaken.TG.Main.Templates;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HeroCreator;
using Awaken.TG.Main.UI.RawImageRendering;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.Main.Utility.UI.Keys;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.Transmogrify {
    public class TransmogrifyUI : GemsBaseUI<VTransmogrifyUI>, IItemChooseParent {
        public override string ContextTitle => LocTerms.TransmogrifyTab.Translate(); 
        public override Type ItemsListUIView => typeof(VItemsListSimpleUI);
        public override Type ItemsListElementView => typeof(VItemsListElement);
        public override Type ItemsCategoryListHostView => typeof(VHostItemsListWithCategoryTransmog);
        public override IEnumerable<ItemsTabType> Tabs => ItemsTabType.Transmog;
        protected override bool TooltipComparerActive => false;
        protected override string GemActionName => LocTerms.TransmogConfirm.Translate();
        public override bool UseCategoryList => true;
        protected override Func<Item, bool> ItemFilter => item => !item.HiddenOnUI && item.IsEquippable && item.Template.CanHaveItemLevel
                                                                  && item.Template.EquipmentType != EquipmentType.QuickUse
                                                                  && item.IsGear && !item.IsArrow && !item.IsMagic;
        protected override int ServiceBaseCost {
            get {
                if (_vTransmogrify == null || VTransmogrifyUI.IsHomeHandcraftingStation) {
                    return 0;
                }
                return GameConstants.Get.transmogCost;
            }
        }

        public IEnumerable<Item> PossibleItems => _tempPossibleItems.ToArray();
        StructList<Item> _tempPossibleItems;
        public Transform ChooseHost => _vTransmogrify.ChooseHost;
        ItemTransmog CurrentTransmog { get; set; }
        HeroRenderer HeroRenderer { get; set; }
        static bool ChooseUIOpened => World.HasAny<TransmogrifyChooseUI>();

        VTransmogrifyUI _vTransmogrify;
        Prompt _clearPrompt;
        Prompt _removePrompt;
        Prompt _changePrompt;
        Item _targetItem;
        ItemInSlots _eqItems;
        ButtonConfig _transmogSlotButton;
        bool _initInProgress;
        
        public new static class Events {
            public static readonly Event<TransmogrifyUI, ItemTransmog> TransmogrifyConfirmed = new(nameof(TransmogrifyConfirmed));
            public static readonly Event<TransmogrifyUI, ItemTransmog> TransmogrifyPreviewChanged = new(nameof(TransmogrifyPreviewChanged));
            public static readonly Event<TransmogrifyUI, bool> TransmogrifyRemoved = new(nameof(TransmogrifyRemoved));
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            ParentModel.View<VGemsUI>().Backgrounds.SetActiveOptimized(false);
            _eqItems = Hero.HeroItems.ItemInSlots;

            this.ListenTo(Events.TransmogrifyPreviewChanged, OnPreviewChanged, this);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<TransmogrifyChooseUI>(), this, OnChooseUIOpened);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<TransmogrifyChooseUI>(), this, OnChooseUIClosed);
        }
        
        protected override void AfterViewSpawned(VTransmogrifyUI view) {
            _vTransmogrify = view;
            LoadAfterViewSpawned().Forget();
        }
        
        async UniTaskVoid LoadAfterViewSpawned() {
            var inProgressBlend = World.SpawnView<VInProgressBlend>(this);
            _initInProgress = true;
            
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            
            base.AfterViewSpawned(_vTransmogrify);
            InitHeroRenderer();
            PrepareKnownItems();

            if (!await AsyncUtil.WaitUntil(this, () => !_initInProgress)) {
                return;
            }
            
            inProgressBlend.Hide();
        }

        protected override void InitPrompts() {
            _selectPrompt = GemsUI.Prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.Select.Translate(), Prompt.Position.First, ControlSchemeFlag.Gamepad), this, !IsEmpty);
            _relicPrompt = Prompts.BindPrompt(Prompt.Hold(KeyBindings.UI.Crafting.CraftOne, GemActionName, GemAction), this, View.GemPrompt, false, false);
            _changePrompt = Prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Crafting.CraftOne, LocTerms.TransmogChange.Translate(), OpenChooseUI), this, _vTransmogrify.ChangePrompt, true, false);
            _removePrompt = Prompts.BindPrompt(Prompt.Hold(KeyBindings.UI.Items.DropItem, LocTerms.TransmogRemove.Translate(), RemoveTransmog), this, _vTransmogrify.RemovePrompt, true, false); 
            _clearPrompt = Prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Items.DropItem, LocTerms.TransmogRestore.Translate(), RemoveTransmog), this, _vTransmogrify.ClearPrompt, true, false); 
            Prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.UIGenericBack.Translate(), Close, Prompt.Position.Last), this);
        }
        
        void InitHeroRenderer() {
            HeroRenderer = AddElement(new HeroRenderer(useLoadoutAnimations: true));
            HeroRenderer.SetViewTargetInstant(HeroRenderer.Target.HeroUITransmog);
            World.SpawnView<VRotator>(HeroRenderer, false, true, _vTransmogrify.RotatorHost);
        }

        public void OnHeroBodyLoaded() {
            HandleHeroWeapons();
        }
        
        void HandleHeroWeapons() {
            _eqItems[EquipmentSlotType.MainHand]?.VisualHeroUnequip();
            _eqItems[EquipmentSlotType.OffHand]?.VisualHeroUnequip();
        }
        
        void PrepareKnownItems() {
            var knownItems = Hero.HeroItems.KnownItems;
            _tempPossibleItems = new StructList<Item>(knownItems.Count);
            
            foreach (var itemTemplateGuid in knownItems) {
                var template = TemplatesUtil.Load<ItemTemplate>(itemTemplateGuid);
                if (template != null && TemplateFilter(template)) {
                    var item = World.Add(new Item(template));
                    _tempPossibleItems.Add(item);
                }
            }
            _initInProgress = false;

            return;

            bool TemplateFilter(ItemTemplate template) => template.IsEquippable 
                                                          && template.EquipmentType != EquipmentType.QuickUse 
                                                          && !template.IsMagic 
                                                          && (template.IsArmor || template.IsShield || template.IsRod || template.IsWeapon);
        }
        
        protected override bool CanRunAction(Item item) {
            return true;
        }

        public void RegisterTransmogSlot(ButtonConfig transmogSlotButton) {
            _transmogSlotButton = transmogSlotButton;
        }
        
        public void OpenChooseUI() {
            if (ChooseUIOpened || _targetItem == null) {
                return;
            }
            
            var chooseUI = AddElement(new TransmogrifyChooseUI(_targetItem));
            GemsUI.ShowEmptyInfo(!chooseUI.IsEmpty, LocTerms.EmptyTransmogrifyInfo.Translate(), LocTerms.EmptyTransmogrifyDesc.Translate());
        }

        void OnChooseUIOpened() {
            _vTransmogrify.FadeLeftSide(0);
            RefreshPrompts();
        }
        
        void OnChooseUIClosed() {
            _vTransmogrify.FadeLeftSide(1);
            RefreshPrompts();
        }
        
        void OnPreviewChanged(ItemTransmog transmog) {
            CurrentTransmog = transmog;
            RefreshPrompts();
        }
        
        protected override void GemAction() {
            if (_targetItem == null) {
                Log.Important?.Error("Transmogrify action cannot be performed without a target item. Critical state.");
                return;
            }
            
            if (_targetItem.TryGetElement<ItemTransmog>(out var existingTransmog) == false){
                Log.Important?.Error($"Target item {_targetItem.ID} does not have ItemTransmog element to confirm transmogrification.");
                return;
            }
            
            PayForService();
            World.Any<TransmogrifyChooseUI>()?.Discard();
            existingTransmog.ConfirmTransmog();

            RefreshItems();
            this.Trigger(Events.TransmogrifyConfirmed, existingTransmog);
        }
        
        void RemoveTransmog() {
            if (CurrentTransmog.HasPreview) {
                CurrentTransmog.RemovePreview();
            } else if (CurrentTransmog.IsTransmogrified) {
                CurrentTransmog.RemoveTransmog();
            }

            RefreshItems();
            this.Trigger(Events.TransmogrifyRemoved, true);
        }

        protected override void OnItemClicked(Item item) {
            base.OnItemClicked(item);
            PreVisualEquip(item, _targetItem);
            
            _targetItem = item;
            CurrentTransmog = item?.TryGetElement<ItemTransmog>();

            VisualEquip();
            RefreshPrompts();
        }

        void PreVisualEquip(Item nextItem, [CanBeNull] Item previousItem) {
            // remove the transmog or previous item that was clicked from the grid
            if (CurrentTransmog != null) {
                CurrentTransmog.Unequip();
            } else {
                previousItem?.VisualHeroUnequip();
            }
            
            var newItemSlotType = nextItem.EquipmentType.MainSlotType;
            _eqItems[newItemSlotType]?.VisualHeroUnequip();
            
            if (previousItem == null) {
                return;
            }
            
            // if slot types are different, handle re-equipping logic
            var previousItemSlotType = previousItem.EquipmentType.MainSlotType;
            if (previousItemSlotType == newItemSlotType) {
                return;
            }

            if (previousItemSlotType.EquipmentCategory == EquipmentCategory.Weapon) {
                HandleHeroWeapons();
            } else {
                _eqItems[previousItemSlotType]?.VisualHeroEquip();
            }
        }
        
        void VisualEquip() {
            if (CurrentTransmog != null) {
                CurrentTransmog.Equip();
            } else {
                _targetItem.VisualHeroEquip();
            }
        }

        void RefreshItems() {
            ItemsUI.SoftRefresh();
            RefreshPrompts();
        }
        
        void RefreshPrompts() {
            bool hasTransmog = CurrentTransmog is { IsTransmogrified: true };
            bool hasPreview = CurrentTransmog is { HasPreview: true };

            _relicPrompt.SetVisible(hasPreview);
            _vTransmogrify.SetCostParentActive(hasPreview && ServiceBaseCost > 0);
            
            _changePrompt.SetVisible(!ChooseUIOpened);
            _clearPrompt.SetVisible(hasPreview);
            _removePrompt.SetVisible(hasTransmog && !hasPreview);
        }
        
        protected override void SpawnItemTooltip() {
            _itemTooltipUI = new ItemTooltipUI(typeof(VTransmogItemTooltipSystem), TooltipParent, 0.2f, comparerActive: TooltipComparerActive);
            AddElement(_itemTooltipUI);
        }

        protected override void OnItemHovered(Item item) {
            base.OnItemHovered(item);
            ItemTooltipUI.SetDescriptor(new ExistingItemDescriptor(HoveredItem));
        }
        
        protected override void OnSelectedItemClickedAgain(Item item) {
            if(_transmogSlotButton != null) {
                World.Only<Focus>().Select(_transmogSlotButton.button);
            }
        }
        
        // Override to prevent the default behavior
        protected override void RefreshItemTooltip() { }
        protected override void SpawnIngredientTooltip() { }
        protected override void OnGamepadSlotSelect(Item item) { }

        protected override void OnDiscard(bool fromDomainDrop) {
            base.OnDiscard(fromDomainDrop);
            ClearPossibleItems();
        }
        
        void ClearPossibleItems() {
            foreach (var tempItem in _tempPossibleItems) {
                tempItem.Discard();
            }
            _tempPossibleItems.Clear();
        }

        protected override void Close() {
            if (HeroRenderer.IsLoading) {
                return;
            }
            GemsUI.Discard();
        }
    }
}
