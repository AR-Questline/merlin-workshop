using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.UIToolkit;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using UnityEngine.UIElements;

namespace Awaken.TG.Main.Locations.Containers {
    [SpawnsView(typeof(VContainerUI))]
    public partial class ContainerUI : Element<Location>, IContainerElementParent {
        public const int WeightCap = int.MaxValue;

        public sealed override bool IsNotSaved => true;

        // Tracks preview items generated and still owned by this container
        readonly Dictionary<Item, ItemSpawningDataRuntime> _previewItems;
        readonly bool _discardLocationOnEmpty;
        
        // === Properties
        public ContainerElement CurrentItem {
            get {
                ContainerElement e = Elements<ContainerElement>().FirstOrDefault(c => c.Selected && !c.WasDiscarded);
                if (e == null) {
                    e = Elements<ContainerElement>().FirstOrDefault();
                    SelectElement(e);
                }
                return e;
            }
        }

        public bool ContainerElementsSpawned { get; private set; }
        public List<ItemSpawningDataRuntime> SpawningDataItems { get; }
        public IInventory Inventory { get; }
        public bool IsIllegal => ItemContainers.Any(e => e.Crime.IsCrime());
        public Crime FirstCrime => ItemContainers.FirstOrDefault(e => e.Crime.IsCrime()).Crime;
        public float CurrentWeight => SpawningDataItems.Sum(i => i?.ItemTemplate?.Weight * i?.quantity ?? 0) + (Inventory?.AllItemsVisibleOnUI().Sum(i => i.Template.Weight * i.Quantity) ?? 0);

        public bool IsEmpty {
            get {
                bool hasAnyItems = SpawningDataItems.Any(data => !data.ItemTemplate.hiddenOnUI);
                bool hasAnyInventoryItems = Inventory != null && Inventory.Items.Any(i => !i.HiddenOnUI);
                return !hasAnyItems && !hasAnyInventoryItems;
            }
        }

        ModelsSet<ContainerElement> ItemContainers => Elements<ContainerElement>();
        PContainerUI PContainerUI => Presenter<PContainerUI>();
        ARAssetReference _containerElementReference;
        PresenterDataProvider PresenterDataProvider => Services.Get<PresenterDataProvider>();
        UIDocumentProvider UIDocumentProvider => Services.Get<UIDocumentProvider>();

        public static class ContainerEvents {
            public static readonly Event<ContainerUI, ContainerUI> ContentChanged = new(nameof(ContentChanged));
        }

        public ContainerUI(IInventory inventory, List<ItemSpawningDataRuntime> items, bool discardLocationOnEmpty) {
            if (items == null) {
                SpawningDataItems = new List<ItemSpawningDataRuntime>();
            } else {
                ItemUtils.MergeStackableItems(items);
                SpawningDataItems = items;
            }

            _previewItems = new(SpawningDataItems.Count);
            _discardLocationOnEmpty = discardLocationOnEmpty;
            Inventory = inventory;
        }

        protected override void OnFullyInitialized() {
            var parent = UIDocumentProvider.TryGetDocument(UIDocumentType.HUD).rootVisualElement;

            var containerUI = new PContainerUI(parent, View<VContainerUI>());
            World.BindPresenter(this, containerUI, OnContainerUIPrepared);
        }

        void OnContainerUIPrepared() {
            _containerElementReference = PresenterDataProvider.containerElementData.BaseData.uxml.GetAndLoad<VisualTreeAsset>(handle => OnContainerElementUIPrepared(handle.Result));
        }

        void OnContainerElementUIPrepared(VisualTreeAsset prototype) {
            PContainerUI.PrepareContainerList(prototype);
            GeneratePreviewItems();
            EnableMainView(true);
        }

        public void SelectNextItem() {
            var currentItem = CurrentItem;
            ContainerElement nextItem = null;
            var find = false;
            foreach (var itemContainer in ItemContainers) {
                if (find) {
                    nextItem = itemContainer;
                    break;
                }
                if (itemContainer == currentItem) {
                    find = true;
                }
            }

            if (nextItem) {
                SelectElement(nextItem);
                PContainerUI.FocusNextItem();
            }
        }

        public void SelectPreviousItem() {
            var currentItem = CurrentItem;
            ContainerElement previousItem = null;
            var find = false;
            foreach (var itemContainer in ItemContainers.Reverse()) {
                if (find) {
                    previousItem = itemContainer;
                    break;
                }
                if (itemContainer == currentItem) {
                    find = true;
                }
            }

            if (previousItem) {
                SelectElement(previousItem);
                PContainerUI.FocusPreviousItem();
            }
        }

        public Item TakeItemFromContainer(ContainerElement containerElement, bool selectNextItem = true, bool vibrate = true) {
            ContainerElement toSelect = null;

            if (vibrate) {
                RewiredHelper.VibrateHighFreq(VibrationStrength.Low, VibrationDuration.VeryShort);
            }

            if (selectNextItem) {
                var currentItem = CurrentItem;

                ContainerElement nextItem = null;
                ContainerElement previousItem = null;
                var itemsEnumerator = ItemContainers.GetEnumerator();

                while (nextItem == null && itemsEnumerator.MoveNext()) {
                    var enumeratorCopy = itemsEnumerator.Copy();

                    if (enumeratorCopy.MoveNext() && enumeratorCopy.Current == currentItem) {
                        previousItem = itemsEnumerator.Current;
                        if (enumeratorCopy.MoveNext()) {
                            nextItem = enumeratorCopy.Current;
                        }
                    }
                }
                toSelect = nextItem ?? previousItem;
            }

            // Ownership was transferred away. Remove from preview list
            Item pickedUpItem = containerElement.Item;
            if (_previewItems.TryGetValue(pickedUpItem, out var val)) {
                SpawningDataItems.Remove(val); // So that container doesn't spawn this item again
                _previewItems.Remove(pickedUpItem);
                pickedUpItem.MarkedNotSaved = false;
            }

            if (containerElement.Crime.IsCrime() && !pickedUpItem.IsStolen && pickedUpItem.Quality != ItemQuality.Quest) {
                pickedUpItem.AddElement(new StolenItemElement(containerElement.Crime));
            }

            containerElement.Crime.TryCommitCrime();

            Item resultItem = pickedUpItem.MoveTo(Hero.Current.Inventory);
            containerElement.Discard();
            ParentModel.Trigger(Location.Events.ItemPickedFromLocation, resultItem);
            ParentModel.Trigger(Location.Events.AnyItemPickedFromLocation, ParentModel);

            if (selectNextItem) {
                SelectElement(toSelect);
            }

            this.Trigger(ContainerEvents.ContentChanged, this);
            if (IsEmpty) {
                World.Only<IHeroInteractionUI>().TriggerChange();
                if (_discardLocationOnEmpty) {
                    ParentModel.Discard();   
                } else {
                    Discard();
                }
            }
            
            return resultItem;
        }

        public void TakeAllItems() {
            RewiredHelper.VibrateHighFreq(VibrationStrength.Medium, VibrationDuration.Short);
            foreach (var item in ItemContainers.ToArraySlow()) {
                TakeItemFromContainer(item, false, false);
            }
        }

        public Item PutItemIntoContainer(Item item) {
            if (CurrentWeight + item.Template.Weight > WeightCap) {
                return null;
            }

            if (item.CanStack) {
                // Do we already have this item as simple data
                var itemReference = SpawningDataItems.FirstOrDefault(i => i?.ItemTemplate == item.Template);

                if (itemReference != null && itemReference.ItemTemplate.CanStack) {
                    itemReference.quantity += item.Quantity;
                } else {
                    // Convert item into simple data
                    var itemData = new ItemSpawningDataRuntime(item);
                    SpawningDataItems.Add(itemData);
                    _previewItems.Add(item, itemData);

                    AddElement(new ContainerElement(item, ParentModel)); // Add local copy
                }
            } else {
                AddElement(new ContainerElement(item, ParentModel)); // Add local copy
            }

            item = item.MoveTo(Inventory);
            this.Trigger(ContainerEvents.ContentChanged, this);
            return item;
        }

        public void TransferItems() {
            if (!HasElement<TransferItems>()) {
                var transferItems = new TransferItems();
                AddElement(transferItems);
                transferItems.ListenTo(Events.AfterDiscarded, () => EnableMainView(false), this);
            }
        }

        public IEnumerable<Item> AllItemsInContainerSorted() {
            return Inventory.AllItemsVisibleOnUI().Where(item => !item.HasElement<NonPickable>()).OrderBy(ItemSortOrder);
        }

        // === Helpers
        void SelectElement(ContainerElement containerElement) {
            if (containerElement == null) {
                return;
            }

            foreach (var itemContainer in ItemContainers.Where(c => c.Selected)) {
                itemContainer.Deselect();
            }

            PContainerUI.Select(containerElement);
            containerElement.Select();
        }

        void GeneratePreviewItems() {
            foreach (var itemSpawningData in SpawningDataItems) {
                if (itemSpawningData?.ItemTemplate == null) {
                    continue;
                }

                var item = World.Add(new Item(itemSpawningData));
                item.MarkedNotSaved = true;
                _previewItems.TryAdd(Inventory.Add(item, false), itemSpawningData);
            }
        }

        void EnableMainView(bool generate) {
            if (HasBeenDiscarded) {
                return;
            }

            var allItemsInContainerSorted = AllItemsInContainerSorted().ToList();
            if (generate) {
                // Only generate the local copies on creation
                foreach (var item in allItemsInContainerSorted) {
                    AddElement(new ContainerElement(item, ParentModel));
                }
            }

            PContainerUI.PopulateItems(allItemsInContainerSorted).Forget();

            ContainerElementsSpawned = true;
            this.Trigger(IContainerElementParent.Events.ContainerElementsSpawned, this);
            SelectElement(Elements<ContainerElement>().FirstOrDefault());
        }

        static int ItemSortOrder(Item item) {
            if (item.IsQuestItem) return 0;
            if (item.IsReadable) return 1;
            if (item.HasElement<ItemToCurrency>()) return 2;
            if (item.IsUnidentified) return 3;
            if (item.HasElement<Lockpick>()) return 4;
            if (item.IsConsumable) return 5;
            if (item.IsCrafting) return 6;
            if (item.IsGem) return 8;
            if (item.IsWeapon) return 10 + ItemQuality.MaxPriority - item.Quality.Priority;
            if (item.IsArmor) return 20 + ItemQuality.MaxPriority - item.Quality.Priority;
            return 7;
        }

        // --- Ignore ItemClicked Here
        public void OnItemClicked(ContainerElement containerElement) { }

        protected override void OnDiscard(bool fromDomainDrop) {
            _containerElementReference?.ReleaseAsset();

            // Cleanup preview items only ones we own should be present
            foreach (Item previewItem in _previewItems.Keys) {
                if (!previewItem.HasBeenDiscarded) {
                    previewItem.Discard();
                }
            }
        }
    }
}