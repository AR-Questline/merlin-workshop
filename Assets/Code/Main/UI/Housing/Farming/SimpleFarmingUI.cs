using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.List;
using Awaken.TG.Main.Heroes.CharacterSheet.Items.Panel.Tabs;
using Awaken.TG.Main.Heroes.Housing.Farming;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.UI.ButtonSystem;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.Main.UI.HUD.AdvancedNotifications.Item;
using Awaken.TG.Main.Utility;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.UI.Housing.Farming {
    [SpawnsView(typeof(VSimpleFarmingUI))]
    public partial class SimpleFarmingUI : Model, IUIStateSource, IItemsUIConfig, IPromptHost, IClosable {
        Prompt _choosePrompt;
        
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown | HUDState.CompassHidden | HUDState.QuestTrackerHidden);

        public IEnumerable<Item> Items => Hero.Current.HeroItems.Items.Where(item => item.HasSeedData);
        public IEnumerable<ItemsTabType> Tabs => new[] { ItemsTabType.All };
        public Transform PromptsHost => View.PromptsHost;
        public Type ItemsUIView => typeof(VItemsEquipmentChooseUI);
        public Type ItemsListUIView => typeof(VItemsList5x3UI);
        Flowerpot Flowerpot { get; }
        VSimpleFarmingUI View => View<VSimpleFarmingUI>();
        Transform IItemsUIConfig.ItemsHost => View.ItemsHost;
        
        public SimpleFarmingUI(Flowerpot flowerpot) {
            Flowerpot = flowerpot;
        }

        protected override void OnFullyInitialized() {
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = true;
            
            var prompts = AddElement(new Prompts(this));
            _choosePrompt = prompts.AddPrompt(Prompt.VisualOnlyTap(KeyBindings.UI.Items.SelectItem, LocTerms.Select.Translate()), this, false, false);
            prompts.AddPrompt(Prompt.Tap(KeyBindings.UI.Generic.Cancel, LocTerms.Close.Translate(), Close), this);
            InitializeItemsUI();
        }

        public static void OpenSimpleFarmingUI(Flowerpot flowerpot) => World.Add(new SimpleFarmingUI(flowerpot));

        void InitializeItemsUI() {
            var itemsUI = AddElement(new ItemsUI(this));
            itemsUI.ListenTo(ItemsUI.Events.ClickedItemsChanged, OnSeedClicked, this);
            itemsUI.ListenTo(ItemsUI.Events.HoveredItemsChanged, OnSeedHovered, this);
            
            OnSeedHovered(itemsUI.Items.LastOrDefault());
            UpdateChoosePrompt(null, true, false);
        }

        void OnSeedHovered(Item item) {
            ItemSeed itemSeed = item?.Element<ItemSeed>();
            if (itemSeed != null) {
                View.SetSeedStats(itemSeed);
            }
            UpdateChoosePrompt(itemSeed, true, true);
        }

        void UpdateChoosePrompt(ItemSeed itemSeed, bool visible, bool active) {
            bool anySlotFree = itemSeed != null && TryGetNextMatchingSlot(itemSeed) != null;
            _choosePrompt.SetupState(visible, active && anySlotFree);
        }

        void OnSeedClicked(Item item) {
            var itemSeed = item.Element<ItemSeed>();
            PlantSlot freeSlot = TryGetNextMatchingSlot(itemSeed);
            var audioFeedback = freeSlot ? CommonReferences.Get.AudioConfig.ButtonAcceptSound : CommonReferences.Get.AudioConfig.LightNegativeFeedbackSound;
            FMODManager.PlayOneShot(audioFeedback);
            if (!freeSlot) {
                return;
            }
            freeSlot.Plant(itemSeed);
            
            if (item is not { Quantity: > 0 }) {
                Element<ItemsUI>().Trigger(ItemsUI.Events.ItemsCollectionChanged, Items);
            }

            UpdateChoosePrompt(itemSeed, true, true);
        }

        PlantSlot TryGetNextMatchingSlot(ItemSeed itemSeed) {
            return Flowerpot.PlantSlots.FirstOrDefault(p => p.plantSize == itemSeed.plantSize && p.State == PlantState.ReadyForPlanting);
        }
        
        public void Close() => Discard();

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Only<ItemNotificationBuffer>().SuspendPushingNotifications = false;
        }
    }
}