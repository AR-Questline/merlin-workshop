using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Slot;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Shops;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.Utility;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems {
    public abstract partial class GemsBaseUI<T> : GemsUITab<T>, IGemBase, IItemsUIConfig, IPromptListener where T : VGemBaseUI {
        Transform _itemsHost;
        
        protected Prompt _relicPrompt;
        protected ItemTooltipUI _ingredientTooltipUI;
        protected ItemTooltipUI _itemTooltipUI;
        
        protected GemsUI GemsUI => ParentModel;
        CurrencyStat HeroWealth => Hero.Wealth;
        CurrencyStat HeroCobweb => Hero.Cobweb;
        Transform IItemsUIConfig.ItemsHost => View.ItemsHost;
        
        public ItemsUI ItemsUI { get; private set; }
        public Item ClickedItem => ItemsUI?.ClickedItemsListElement?.Item;
        
        protected ItemsListElementUI ItemsListElementUI => ItemsUI.ClickedItemsListElement;
        protected VGemBaseUI View => View<VGemBaseUI>();
        protected Transform TooltipParent => GemsUI.TooltipParent;
        protected Transform TooltipParentStatic => GemsUI.TooltipParentStatic;
        protected Hero Hero => Hero.Current;
        [UnityEngine.Scripting.Preserve] protected Item HoveredItem => ItemsUI?.HoveredItem;

        protected virtual bool TooltipComparerActive => false;
        protected virtual int CostMultiplier => 1;

        protected abstract int ServiceBaseCost { get; }
        protected virtual int CobwebServiceBaseCost { get; set; } = 0;
        protected abstract string GemActionName { get; }
        protected abstract bool CanRunAction(Item item);
        protected abstract Func<Item, bool> ItemFilter { get; }
        protected abstract void GemAction();
        
        public IEnumerable<Item> Items => AllHeroItems.Where(ItemFilter);
        public IEnumerable<Item> AllHeroItems => Hero.HeroItems.Items.Concat(Hero.Storage.Items);
        public List<SimilarItemsData> SimilarItemsData { get; } = new();
        public virtual IEnumerable<CountedItem> Ingredients => null;

        public virtual bool UseDefaultTab => false;
        public virtual bool UseCategoryList => false;
        public virtual bool UseFilter => false;
        public virtual ItemTooltipUI ItemTooltipUI => _itemTooltipUI;
        public virtual ItemTooltipUI IngredientTooltipUI => _ingredientTooltipUI;
        public virtual Type ItemsListUIView => typeof(VItemsListDefaultUI);
        public virtual Type ItemsListElementView => typeof(VItemUpgradesElement);
        public virtual int ServiceCost => ServiceBaseCost * CostMultiplier;
        public virtual int CobwebServiceCost => CobwebServiceBaseCost * CostMultiplier;
        public virtual IEnumerable<ItemsTabType> Tabs => ItemsTabType.Relics;
        public virtual string ContextTitle => LocTerms.UIItemsEmptyGemSlot.Translate();
        public Prompts Prompts => GemsUI.Prompts;
        public bool AllowMultipleClickEventsOnTheSameItem => false;
        bool IsEmpty => !Items.Any();

        readonly EventReference _audioButtonClick = CommonReferences.Get.AudioConfig.ButtonClickedSound;
        
        public bool CanAfford(CurrencyType currencyType) {
            return currencyType == CurrencyType.Money ? HeroWealth >= ServiceCost : HeroCobweb >= CobwebServiceCost;
        }

        public bool CanAffordAll() {
            return HeroWealth >= ServiceCost && HeroCobweb >= CobwebServiceCost;
        }
        
        protected virtual void OnItemClicked(Item item) {
            if (HasBeenDiscarded) return;

            FMODManager.PlayOneShot(_audioButtonClick);
            RefreshPrompt(item);
            RefreshItemTooltip();
            this.Trigger(IGemBase.Events.ClickedItemChanged, item);
        }

        protected virtual void OnSelectedItemClickedAgain(Item item) { }

        protected virtual bool CheckIngredients() {
            return Ingredients.CheckSimilarItemsPossession(SimilarItemsData);
        }

        protected virtual void SpawnItemTooltip() {
            _itemTooltipUI = new ItemTooltipUI(typeof(VGemTooltipSystemUI), TooltipParentStatic, isStatic: true, comparerActive: TooltipComparerActive);
            AddElement(_itemTooltipUI);
        }

        protected virtual void SpawnIngredientTooltip() {
            _ingredientTooltipUI = new SharpeningIngredientTooltipUI(typeof(VCraftingTooltipSystemUI), TooltipParent, 0.1f, 0.1f, comparerActive: false);
            AddElement(_ingredientTooltipUI);
        }

        protected override void AfterViewSpawned(T view) {
            ItemsUI = AddElement(new ItemsUI(this));
            ItemsUI.ListenTo(ItemsUI.Events.ClickedItemsChanged, OnItemClicked, this);
            ItemsUI.ListenTo(ItemsUI.Events.ClickedItemTriggered, OnSelectedItemClickedAgain, this);
            ItemsUI.ListenTo(ItemsUI.Events.HoveredItemsChanged, OnGamepadSlotSelect, this);
            _relicPrompt = GemsUI.Prompts.BindPrompt(Prompt.Tap(KeyBindings.UI.Crafting.CraftOne, GemActionName, GemAction), this, View.GemPrompt, false);
            _relicPrompt.AddListener(this);
            SpawnItemTooltip();
            SpawnIngredientTooltip();
            View.HideRightSide();
            ItemsUI.FocusFirstItem().Forget();
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = true;
            SimilarItemsData.FillSimilarItemsDataList(AllHeroItems);
            GemsUI.ShowEmptyInfo(!IsEmpty, LocTerms.EmptyNoItems.Translate(), LocTerms.EmptyBagDesc.Translate());
            World.EventSystem.ListenTo(EventSelector.AnySource, ItemsTabs.TabEvents.Events.TabsChanged, this, OnTabChanged);
        }
        
        protected void Refresh() {
            if (TryToDiscardListElement()) {
                View.HideRightSide();
            }
            
            RefreshPrompt(ClickedItem);
            RefreshItemTooltip();
            
            ItemsUI.Trigger(ItemsUI.Events.ItemsCollectionChanged, Items);
            GemsUI.ShowEmptyInfo(!IsEmpty, LocTerms.EmptyNoItems.Translate(), LocTerms.EmptyBagDesc.Translate());
            SimilarItemsData.FillSimilarItemsDataList(AllHeroItems);
            this.Trigger(IGemBase.Events.AfterRefreshed, true);
        }

        protected virtual void RefreshItemTooltip() {
            ItemTooltipUI.SetDescriptor(new ExistingItemDescriptor(ClickedItem));
        }

        public void RefreshPrompt(bool state) {
            _relicPrompt.SetActive(state);
        }

        void RefreshPrompt(Item item) {
            _relicPrompt.SetActive(CanAffordAll() && CanRunAction(item));
        }

        protected virtual void OnGamepadSlotSelect(Item item) {
            // simulate click on gamepad when navigating to a slot
            if (item != null && RewiredHelper.IsGamepad) {
                ItemsUI.ForceClick(item);
            }
        }
        
        protected virtual void OnTabChanged(ItemsListUI itemsListUI) { }
        
        bool TryToDiscardListElement() {
            if (!ItemFilter(ClickedItem)) {
                var element = ItemsUI.GetItemsListElementWithItem(ClickedItem);
                element?.Discard(); 
                return true;
            }

            return false;
        }
        
        protected void PayForService() {
            HeroWealth.DecreaseBy(ServiceCost);
            HeroCobweb.DecreaseBy(CobwebServiceCost);
            DropIngredients();
            SimilarItemsData.FillSimilarItemsDataList(AllHeroItems);
            this.Trigger(IGemBase.Events.GemActionPerformed, true);
        }

        void DropIngredients() {
            if (Ingredients == null || !Ingredients.Any()) {
                return;
            }
            
            foreach ((ItemTemplate itemTemplate, int requiredQuantity) in Ingredients) {
                SimilarItemsData.DropHeroSimilarItems(itemTemplate, requiredQuantity);
            }
        }

        public void SetName(string name) { }
        public void SetActive(bool active) { }
        public void SetVisible(bool visible) { }
        
        public void OnHoldKeyHeld(Prompt source, float percent) {
            RewiredHelper.VibrateLowFreq(VibrationStrength.Low, VibrationDuration.Continuous);
        }

        public void OnHoldKeyUp(Prompt source, bool completed = false) {
            if (completed) {
                RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.Short);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = false;
        }
    }
}