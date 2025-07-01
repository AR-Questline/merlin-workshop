using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Locations.Containers {
    [SpawnsView(typeof(VHeroTransferItems))]
    public partial class HeroTransferItems : Element<TransferItems>, IContainerElementParent {
        public bool ContainerElementsSpawned { get; private set; }
        public ContainerUI ContainerUI => ParentModel.ParentModel;
        ICharacterInventory Inventory => Hero.Current.HeroItems;
        IEnumerable<Item> Items => Inventory.ContainedItems().Where(c => !c.HiddenOnUI);

        protected override void OnFullyInitialized() {
            foreach (var item in Items) {
                AddNewItem(item);
            }

            ContainerElementsSpawned = true;
            this.Trigger(IContainerElementParent.Events.ContainerElementsSpawned, this);
            
            this.ListenTo(TransferItems.Events.ItemTransferred, AddNewItem, this);
        }

        public void FocusButton() {
            if (HasElement<ContainerElement>()) {
                //A different way of marking the focus is required for elements rewritten in the UI Toolkit.
                //World.Only<Focus>().Select(Elements<ContainerElement>().First().View<VContainerElement>().arButton);
            }
        }

        void AddNewItem(Item newItem) {
            AddElement(new ContainerElement(newItem, ContainerUI.ParentModel));
        }

        public void OnItemClicked(ContainerElement containerElement) {
            ParentModel.PutItemToContainer(containerElement);
        }
    }
}