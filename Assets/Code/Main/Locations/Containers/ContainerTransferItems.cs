using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Containers {
    [SpawnsView(typeof(VContainerTransferItems))]
    public partial class ContainerTransferItems : Element<TransferItems>, IContainerElementParent {
        public sealed override bool IsNotSaved => true;

        public bool ContainerElementsSpawned { get; private set; }
        public ContainerUI ContainerUI => ParentModel.ParentModel;
        public IInventory Inventory => ParentModel.ContainerInventory;

        protected override void OnFullyInitialized() {
            foreach (var item in Inventory.AllItemsVisibleOnUI()) {
                AddNewItem(item);
            }

            ContainerElementsSpawned = true;
            this.Trigger(IContainerElementParent.Events.ContainerElementsSpawned, this);
            
            this.ListenTo(TransferItems.Events.ItemTransferred, AddNewItem, this);
        }

        public void FocusButton() {
            if (HasElement<ContainerElement>()) {
                //If the items transfer logic has been restored, you should change way of marking the focus for the UIToolkit.
                //World.Only<Focus>().Select(Elements<ContainerElement>().First().View<VContainerElement>().arButton);
            }
        }

        void AddNewItem(Item newItem) {
            AddElement(new ContainerElement(newItem, ContainerUI.ParentModel));
        }

        public void OnItemClicked(ContainerElement containerElement) {
            ParentModel.PutToHeroInventory(containerElement);
        }
    }
}