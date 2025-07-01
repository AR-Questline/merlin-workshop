using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Crafting.Cooking;
using Awaken.TG.Main.Crafting.HandCrafting.IngredientsView;
using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.Crafting.Slots;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Descriptors;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.EmptyContent;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.UI.Popup;
using Awaken.TG.Main.UI.Popup.PopupContents;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using FMODUnity;
using UnityEngine;
using ItemData = Awaken.TG.Main.Heroes.Items.ItemData;

namespace Awaken.TG.Main.Crafting {
    public abstract partial class Crafting : Element<CraftingTabsUI>, IClosable, ICrafting {
        const int MaxCraftingSlots = 3;
        
        protected CraftingResult _recentCraftingResult;
        TempItemDescriptor _tempItemDescriptor;
        PopupUI _popup;
        
        public List<SimilarItemsData> SimilarItemsData { get; } = new();
        public Hero Hero { get; }
        public CraftingTemplate GenericTemplate { get; }
        public CraftingItemTooltipUI PossibleResultTooltipUI { get; set; }
        
        public virtual Func<Item, bool> ItemFilter => _ => true;

        public virtual float TooltipAppearDelay => 0f;
        public virtual float TooltipTweenTime => 0f;
        public virtual bool TooltipPreventDisappearing => true;

        public abstract bool ButtonInteractability { get; }
        public Transform WorkbenchParent => View<IVCrafting>().WorkbenchParent;
        public virtual Transform InventoryParent => View<IVCrafting>().InventoryParent;

        public HeroItems HeroItems => Hero.HeroItems;
        public ModelsSet<InventorySlot> InventorySlots => Elements<InventorySlot>();
        public ModelsSet<WorkbenchSlot> WorkbenchSlots => Elements<WorkbenchSlot>();

        public IEnumerable<CraftingItem> WorkbenchCraftingItems => WorkbenchSlots.Select(x => x.CraftingItem).WhereNotNull();
        public IEnumerable<ItemData> WorkbenchItemsData => WorkbenchSlots.Select(x => (ItemData)x.CraftingItem).Where(i => i.item != null);
        public IEnumerable<IRecipe> Recipes => GenericTemplate.Recipes.Where(r => r?.Outcome != null).Where(KnownRecipe);
        public virtual EventReference CraftCompletedSound => CommonReferences.Get.AudioConfig.CraftingAudio.GetResultSound(_recentCraftingResult.quality);

        protected abstract bool KnownRecipe(IRecipe recipe);
        
        // === Events
        public new static class Events {
            public static readonly Event<Hero, CreatedEvent> Created = new(nameof(Created));
            public static readonly Event<Hero, CreatedEvent> CreatedAddedToHero = new(nameof(CreatedAddedToHero));
            public static readonly Event<ICrafting, IRecipe> OnRecipeCrafted = new(nameof(OnRecipeCrafted));
        }
        
        public IRecipe CurrentRecipe { get; protected set; }
        public IEnumerable<Item> FilteredHeroItems => HeroItems.Items.Concat(Hero.Storage.Items).Where(ItemFilter);

        protected Crafting(Hero hero, CraftingTemplate genericTemplate) {
            Hero = hero;
            GenericTemplate = genericTemplate;
            SimilarItemsData.FillSimilarItemsDataList(FilteredHeroItems);
        }

        protected SelectedWorkbenchSlot _currentSlot;

        // === Lifecycle
        
        public virtual void Init() {
            RefreshWorkbenchSlots();
            _currentSlot = new SelectedWorkbenchSlot(this, WorkbenchSlots);
        }

        protected void RefreshWorkbenchSlots() {
            AddWorkbenchSlots();

            foreach (var slot in WorkbenchSlots) {
                slot.RemoveElementsOfType<CraftingItem>();
            }
        }

        protected void AddWorkbenchSlots() {
            //Make sure there are 3 crafting slots
            while (WorkbenchSlots.CountLessThan(MaxCraftingSlots)) {
                AddElement(CreateWorkbenchSlot());
            }
        }

        protected virtual CraftingSlot CreateWorkbenchSlot() {
            return new WorkbenchSlot();
        }

        protected void DiscoverRecipe(IRecipe recipe) {
            Hero.Element<HeroRecipes>().LearnRecipe(recipe);
        }
        
        protected bool IsLearned(IRecipe recipe) {
            return Hero.Element<HeroRecipes>().IsLearned(recipe);
        }
        
        public void ShowEmptyInfo(bool active) {
            if (!active) {
                PossibleResultTooltipUI?.ForceDisappear();
                OverrideLabels(View<IEmptyInfo>());
            }
            
            this.Trigger(IEmptyInfo.Events.OnEmptyStateChanged, active);
        }
        
        public virtual void OverrideLabels(IEmptyInfo infoView) { }

        // === Player Action Reactors

        /// <summary>
        /// Selects CraftingItem for the currently selected slot
        /// </summary>
        /// <param name="interactableItem"></param>
        public virtual void AddToWorkbenchSlot(InteractableItem interactableItem) {
            if (interactableItem == null) return;

            if (_currentSlot.GoToEmpty()) {
                _currentSlot.Current.AddElement(interactableItem.TakePart(1, false));
                _currentSlot.MoveNext();
                OnWorkbenchSlotsChange();
            }
        }

        public void RemoveFromWorkbenchSlot(InteractableItem interactableItem) {
            if (interactableItem == null) return;
            if (TryGetElement<IngredientsGridUI>() is { } ingredientsGridUI) {
                foreach (var inventoryItem in InventorySlots.Select(slot => slot.TryGetElement<InteractableItem>())) {
                    if (inventoryItem == null) continue;
                    if (inventoryItem.CanAddPart(interactableItem)) {
                        inventoryItem.AddPart(interactableItem.TakePart(1, true));
                        OnWorkbenchSlotsChange();
                        return;
                    }
                }

                var newSlot = new InventorySlot(ingredientsGridUI.IngredientTabContents.Index++, interactableItem.TakePart(1, true));
                AddElement(newSlot);
                OnWorkbenchSlotsChange();
            }
        }

        // === Overrideable

        protected virtual void OnWorkbenchSlotsChange() { }

        protected void OnPlayerCancelAction() {
            ParentModel.Close();
        }

        // === Creation 
        public void Create(int quantityMultiplier = 1) {
            Item itemForHero = null;
            for (int i = 0; i < quantityMultiplier; i++) {
                var currentRecipe = CurrentRecipe;
                var usedTemplates = WorkbenchItemsData.Select(i => i.item.Template).ToArray();
                _recentCraftingResult = DetermineCraftingResult();
                itemForHero = _recentCraftingResult.item;
                Hero.Trigger(Events.Created, new CreatedEvent(GenericTemplate, itemForHero, currentRecipe, usedTemplates));
                itemForHero = HeroItems.Add(itemForHero);
                Hero.Trigger(Events.CreatedAddedToHero, new CreatedEvent(GenericTemplate, itemForHero, currentRecipe, usedTemplates));
            }
            DropIngredients(quantityMultiplier);
            AfterCreate(itemForHero);
        }

        /// <summary>
        /// Item here might be already discarded (if it's stacked) 
        /// </summary>
        protected virtual void AfterCreate(Item item) {
            CurrentRecipe.StartStoryOnCreation();
            this.Trigger(Events.OnRecipeCrafted, CurrentRecipe);
            SimilarItemsData.FillSimilarItemsDataList(FilteredHeroItems);
        }

        void DropIngredients(int quantityMultiplier = 1) {
            foreach (CraftingItem craftingItem in WorkbenchCraftingItems) {
                craftingItem.Drop(quantityMultiplier);
            }
        }

        protected virtual CraftingResult DetermineCraftingResult() {
            return new CraftingResult(CurrentRecipe.Create(this));
        }

        public void CreateMany(Action callback, int quantity = 1) {
            var inputItemQuantityUI = new InputItemQuantityUI(upperBound: CraftableItemsCount(), quantityMultiplayer: quantity);
            AddElement(inputItemQuantityUI);
            var popupContent = new DynamicContent(inputItemQuantityUI, typeof(VInputItemQuantityUI));
            
            _popup = PopupUI.SpawnSimplePopup(typeof(VSmallPopupUI),
                string.Empty,
                PopupUI.AcceptTapPrompt(() => {
                    callback?.Invoke();
                    Create(inputItemQuantityUI.Value);
                    ClosePopup();
                }),
                PopupUI.CancelTapPrompt(ClosePopup),
                LocTerms.Quantity.Translate(),
                popupContent
            );
            return;

            void ClosePopup() {
                inputItemQuantityUI.Discard();
                inputItemQuantityUI = null;
                _popup?.Discard();
                _popup = null;
            }
        }

        public int CraftableItemsCount() {
            using var slotsEnumerator = WorkbenchSlots.GetManagedEnumerator();
            var ghostItems = slotsEnumerator.SelectMany(s => s.Elements<GhostItem>().GetManagedEnumerator()).ToArray();
            var groupedItems = ghostItems.GroupBy(ghostItem => ghostItem.Item);
            int maxCraftableItems = int.MaxValue;

            foreach (var group in groupedItems) {
                int inventoryQuantity = group.First().inventoryQuantity;
                int totalItemsQuantity = group.Sum(ghost => ghost.requiredQuantity);
                int maxCraftableItemsByIngredient = inventoryQuantity / totalItemsQuantity;
                if (maxCraftableItemsByIngredient < maxCraftableItems) {
                    maxCraftableItems = maxCraftableItemsByIngredient;
                }
            }

            return maxCraftableItems;
        }

        public void RefreshTooltipDescriptor(int level) {
            _tempItemDescriptor?.Dispose();
            _tempItemDescriptor = new TempItemDescriptor(CurrentRecipe, this, level);
            PossibleResultTooltipUI.SetDescriptor(_tempItemDescriptor);
        }
        
        // === IClosable
        
        public void Close() { //On cancel button pressed
            RefreshWorkbenchSlots();
            OnPlayerCancelAction();
        }

        // === ICraftingTabContents
        public abstract Type TabView { get; }

        public virtual void AfterViewSpawned(View view) {
            Init();
            AddElement(new ItemTooltipUI(typeof(VCraftingTooltipSystemUI), View<IVCrafting>().transform, 0.1f, 0.1f, comparerActive: false));
            PossibleResultTooltipUI = new CraftingItemTooltipUI(typeof(VCraftingTooltipSystemUI), View<IVCrafting>().StaticTooltip, TooltipAppearDelay, 
                TooltipAppearDelay, TooltipTweenTime, true, false, TooltipPreventDisappearing);
            AddElement(PossibleResultTooltipUI);
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = true;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = false;
        }
    }

    public abstract partial class Crafting<T> : Crafting where T : CraftingTemplate {
        public T Template { get; }

        protected Crafting(Hero hero, T genericTemplate) : base(hero, genericTemplate) {
            Template = genericTemplate;
        }
    }
}
