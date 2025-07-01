using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.UI.Handlers.Focuses;
using Awaken.TG.MVC.UI.Handlers.States;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Containers {
    [SpawnsView(typeof(VTransferItems))]
    public partial class TransferItems : Element<ContainerUI>, IUIStateSource, IClosable {
        public sealed override bool IsNotSaved => true;

        // --- IUIStateSource
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown);
        public List<ItemSpawningDataRuntime> ContainerItems => ParentModel.SpawningDataItems;
        public IInventory ContainerInventory => ParentModel.Inventory;
        HeroTransferItems _heroTransferItems;
        ContainerTransferItems _containerTransferItems;

        // === Events
        public new static class Events {
            public static readonly Event<IContainerElementParent, Item> ItemTransferred = new(nameof(ItemTransferred));
            public static readonly Event<ContainerUI, ItemTemplate> WeightCapReached = new(nameof(WeightCapReached));
        }
        
        protected override void OnInitialize() {
            _heroTransferItems = AddElement(new HeroTransferItems());
            _containerTransferItems = AddElement(new ContainerTransferItems());
        }

        protected override void OnFullyInitialized() {
            AsyncOnFullyInitialized().Forget();
        }

        protected async UniTaskVoid AsyncOnFullyInitialized() {
            if (!await AsyncUtil.WaitUntil(
                    ParentModel,
                    () =>
                        _containerTransferItems.HasElement<ContainerElement>() ||
                        _heroTransferItems.HasElement<ContainerElement>())) {
                return;
            }
            SetFocus().Forget();
        }

        public void PutToHeroInventory(ContainerElement containerElement) {
            var heroItem = ParentModel.TakeItemFromContainer(containerElement, false);
            
            if (heroItem == null) return;
            GenerateVisualInContainer(_heroTransferItems, heroItem);
        }

        public void PutItemToContainer(ContainerElement containerElement) {
            if (ParentModel.CurrentWeight + containerElement.ItemTemplate.Weight > ContainerUI.WeightCap) {
                ParentModel.Trigger(Events.WeightCapReached, containerElement.ItemTemplate);
                return;
            }
            Item containerItem = ParentModel.PutItemIntoContainer(containerElement.Item);
            containerElement.Discard();
            if (containerItem == containerElement.Item) {
                GenerateVisualInContainer(_containerTransferItems, containerItem);
            }
        }

        void GenerateVisualInContainer(IContainerElementParent targetForNew, Item newItem) {
            TriggerChange();
            targetForNew.Trigger(Events.ItemTransferred, newItem);
            SetFocus().Forget();
        }

        async UniTaskVoid SetFocus() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            if (World.Only<Focus>().Focused == null) {
                if (ContainerItems.Count > 0 || ContainerInventory.Items.Any()) {
                    Element<ContainerTransferItems>().FocusButton();
                } else {
                    Element<HeroTransferItems>().FocusButton();
                }
            }
        }
        
        public void Close() {
            if (ParentModel.IsEmpty) {
                ParentModel.Discard();
            } else {
                Discard();
            }
        }
    }
}