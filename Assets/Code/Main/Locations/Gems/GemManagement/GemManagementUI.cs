using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Choose;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Gems;
using Awaken.TG.Main.Heroes.Items.Tooltips;
using Awaken.TG.Main.Heroes.Items.Tooltips.Views;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.Utility;
using FMODUnity;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Gems.GemManagement {
    public partial class GemManagementUI : GemsBaseUI<VGemManagementUI>, IItemChooseParent {
        VGemManagementUI _view;
        GemSlotUI _clickedGemSlot;
        
        Dictionary<GemModificationState, GemManagementData> GemManagementDataMap { get; set; } = new();
        
        public IEnumerable<Item> PossibleItems => AllHeroItems.Where(i => !i.HiddenOnUI);
        public Transform ChooseHost => _view.ChooseHost;
        
        public override Type ItemsListUIView => typeof(VItemsListSimpleUI);
        public override string ContextTitle => LocTerms.ManageRelicsTab.Translate();

        protected override int ServiceBaseCost {
            get {
                if (State == GemModificationState.AttachGem) {
                    var gems = (int)Elements<GemSlotUI>().Count(g => g.IsBeingPreviewed);
                    return gems * GemManagementDataMap[State].serviceCost;
                }

                return GemManagementDataMap.TryGetValue(State, out var gemData) ? gemData.serviceCost : 0;
            }
        }

        protected override string GemActionName => GemManagementDataMap[State].gemActionName;
        protected override Func<Item, bool> ItemFilter => item => !item.HiddenOnUI && item.CanHaveRelics && !item.IsMagic &&
                                                                  (item.IsArmor || item.IsWeapon);
        GemModificationState State { get; set; } = GemModificationState.None;
        
        public new static class Events {
            [UnityEngine.Scripting.Preserve] public static readonly Event<GemManagementUI, Item> ItemClicked = new(nameof(ItemClicked));
            public static readonly Event<GemManagementUI, Item> GemSlotUnlocked = new(nameof(GemSlotUnlocked));
            public static readonly Event<GemManagementUI, GemAttachmentChange> GemAttached = new(nameof(GemAttached));
            public static readonly Event<GemManagementUI, GemAttachmentChange> GemDetached = new(nameof(GemDetached));
        }
        
        protected override void AfterViewSpawned(VGemManagementUI view) {
            InitializeGemManagementData();
            _view = view;
            base.AfterViewSpawned(_view);
            this.ListenTo(Model.Events.AfterElementsCollectionModified, el => OnGemChosen(el as GemChooseUI), this);
            World.EventSystem.ListenTo(EventSelector.AnySource, GemSlotUI.Events.GemSlotClicked, this, OnGemSlotClicked);
        }

        protected override void OnTabChanged(ItemsListUI itemsListUI) {
            if (itemsListUI != null && itemsListUI.ParentModel == ItemsUI) {
                _clickedGemSlot = null;
                ChangeModificationState(GemModificationState.None);
                this.Trigger(IGemBase.Events.ClickedItemChanged, null);
            }
        }

        // probably could be something more sophisticated
        void InitializeGemManagementData() {
            GemManagementDataMap = new Dictionary<GemModificationState, GemManagementData> {
                {
                    GemModificationState.None,
                    new GemManagementData {
                        gemAction = null,
                        gemActionName = string.Empty,
                        serviceCost = 0
                    }
                }, {
                    GemModificationState.UnlockSlot,
                    new GemManagementData {
                        gemAction = UnlockGemSlot,
                        gemActionName = LocTerms.RelicsPromptAddSlot.Translate(),
                        serviceCost = GemUtils.AddGemSlotCost,
                        actionAudioEvent = CommonReferences.Get.AudioConfig.ButtonAcceptSound
                    }
                }, {
                    GemModificationState.AttachGem,
                    new GemManagementData {
                        gemAction = AttachGem,
                        gemActionName = LocTerms.RelicsPromptAttach.Translate(),
                        serviceCost = GemUtils.AttachGemCost,
                        actionAudioEvent = CommonReferences.Get.AudioConfig.ButtonAcceptSound
                    }
                }, {
                    GemModificationState.RetrieveGem,
                    new GemManagementData {
                        gemAction = RetrieveGem,
                        gemActionName = LocTerms.RelicsPromptRetrieve.Translate(),
                        serviceCost = GemUtils.RetrieveGemSlotCost
                    }
                },
            };
        }

        void OnGemSlotClicked(GemSlotUI gemSlotUI) {
            _clickedGemSlot = gemSlotUI;

            if (!gemSlotUI.IsUnlocked) {
                ChangeModificationState(GemModificationState.UnlockSlot);
                return;
            }

            if (gemSlotUI.IsUnlocked && !gemSlotUI.HasGemAttached) {
                var gemChooseUI = AddElement(new GemChooseUI(gemSlotUI));
                GemsUI.ShowEmptyInfo(!gemChooseUI.IsEmpty, LocTerms.EmptyRelicsInfo.Translate(), LocTerms.EmptyRelicsDesc.Translate());
                return;
            }

            if (gemSlotUI.IsUnlocked && gemSlotUI.HasGemAttached) {
                ChangeModificationState(GemModificationState.RetrieveGem);
            }
        }

        protected override void OnItemClicked(Item item) {
            if (HasBeenDiscarded) {
                return;
            }
            
            FMODManager.PlayOneShot(CommonReferences.Get.AudioConfig.ButtonClickedSound);
            _clickedGemSlot = null;
            ChangeModificationState(GemModificationState.None);
            this.Trigger(IGemBase.Events.ClickedItemChanged, item);
        }

        protected override void OnSelectedItemClickedAgain(Item item) {
            World.Only<Focus>().Select(ItemsListElementUI.NextFocusTarget?.Invoke());
        }

        void FadeLeftSide(float targetAlpha) {
            _view.FadeLeftSide(targetAlpha);
        }

        void ChangeModificationState(GemModificationState state) {
            State = state;
            RefreshActionState();
        }

        protected override bool CanRunAction(Item _) {
            return State != GemModificationState.None;
        }
        
        protected override void OnGamepadSlotSelect(Item item) {
            // override to prevent the default behavior - we don't want to click the item unnecessarily
        }

        protected override void GemAction() {
            var audioFeedbackEvent = GemManagementDataMap[State].actionAudioEvent;
            if (!audioFeedbackEvent.IsNull) { 
                FMODManager.PlayOneShot(audioFeedbackEvent);
            }
            
            PayForService();
            GemManagementDataMap[State].gemAction?.Invoke();
            
            bool isSomeGemInPreview = Elements<GemSlotUI>().Any(s => s.IsBeingPreviewed);
            ChangeModificationState(isSomeGemInPreview ? GemModificationState.AttachGem : GemModificationState.None);
        }

        void UnlockGemSlot() {
            Item gearItem = ClickedItem;
            ItemGems itemGems = gearItem.TryGetElement<ItemGems>();
            bool isNewlyCreated = false;
                
            if (itemGems == null) {
                itemGems = new ItemGems(1, gearItem.MaxGemSlots);
                gearItem.AddElement(itemGems);
                isNewlyCreated = true;
            }

            if (!isNewlyCreated && itemGems is {CanIncreaseSlots: true}) {
                itemGems.IncreaseLimit();
            }
            
            _clickedGemSlot.UnlockGemSlot();
            this.Trigger(Events.GemSlotUnlocked, gearItem);
        }

        void AttachGem() {
            foreach (GemSlotUI gemSlot in Elements<GemSlotUI>().Where(s => s is {IsUnlocked: true, IsBeingPreviewed: true})) {
                gemSlot.AttachGem();
            }
        }
        
        void RetrieveGem() {
            Hero.HeroItems.Add(_clickedGemSlot.RetrieveGem());
        }

        protected override void SpawnItemTooltip() {
            _itemTooltipUI = new ItemTooltipUI(typeof(VItemTooltipSystemUI), TooltipParent, 0.2f, isStatic: false, comparerActive: TooltipComparerActive);
            AddElement(_itemTooltipUI);
        }
        
        void RefreshActionState() {
            bool canRunAction = CanRunAction(null);
            _relicPrompt.SetupState(canRunAction, CanAffordAll() && canRunAction);
            _relicPrompt.ChangeName(GemActionName);
            _view.UpdateCostValue(ServiceBaseCost, canRunAction);
            this.Trigger(IGemBase.Events.CostRefreshed, true);
        }

        void OnGemChosen(GemChooseUI gemChoose) {
            if (gemChoose == null) {
                return;
            }

            FadeLeftSide(gemChoose.HasBeenDiscarded ? 1f : 0f);
            bool shouldBeAttachState = gemChoose.HasBeenDiscarded && Elements<GemSlotUI>().Any(s => s.IsBeingPreviewed);
            ChangeModificationState(shouldBeAttachState ? GemModificationState.AttachGem : GemModificationState.None);
        }

        struct GemManagementData {
            public Action gemAction;
            public string gemActionName;
            public int serviceCost;
            public EventReference actionAudioEvent;
        }

        enum GemModificationState : byte {
            None,
            UnlockSlot,
            AttachGem,
            RetrieveGem,
        }
        
        public struct GemAttachmentChange {
            public Item item;
            public Item gem;
            public bool attached;
            
            public GemAttachmentChange(Item item, Item gem, bool attached) {
                this.item = item;
                this.gem = gem;
                this.attached = attached;
            }
        }
    }
}