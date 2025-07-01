using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Wyrdnessing;
using Awaken.TG.Utility.Attributes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Serialization;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Locations.Actions {
    public sealed partial class SearchAction : AbstractLocationAction {
        public override ushort TypeForSerialization => SavedModels.SearchAction;

        // === Fields & Properties
        [Saved] List<ItemSpawningDataRuntime> _itemsInsideContainer;
        [Saved] IInventory _backupInventory;
        
        IEnumerable<ILootTable> _lootTables;
        IEnumerable<ILootTable> _corpseLoot;
        IEnumerable<ILootTable> _wyrdConvertedLoot;
        
        [Saved] bool _discardLocationOnEmpty;

        public bool SearchAvailable { get; private set; } = true;
        public IEnumerable<ItemTemplate> AvailableTemplates {
            get {
                IEnumerable<ItemTemplate> templates = _itemsInsideContainer.Select(i => i.ItemTemplate);
                IEnumerable<ItemTemplate> inventoryTemplates = RetrieveInventory()?.Items.Select(i => i.Template);
                return inventoryTemplates != null ? templates.Concat(inventoryTemplates) : templates;
            }
        }
        
        public override bool IsIllegal => Crime.Theft(_itemsInsideContainer[0], ParentModel).IsCrime();

        public override InfoFrame ActionFrame => IsEmpty()
            ? new InfoFrame($"{LocTerms.Search.Translate()} ({LocTerms.Empty.Translate()})", false)
            : InfoFrame.Empty;

        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        SearchAction() {}
        
        public SearchAction(IEnumerable<ILootTable> loot, List<ItemSpawningData> additionalItems = null, object debugTarget = null, bool discardLocationOnEmpty = false) {
            _lootTables = loot;
            _itemsInsideContainer = new List<ItemSpawningDataRuntime>();
            if (additionalItems != null) {
                var finalDebugTarget = debugTarget ?? this;
                foreach (var item in additionalItems) {
                    if (item.itemTemplateReference is { IsSet: true }) {
                        _itemsInsideContainer.Add(item.ToRuntimeData(finalDebugTarget));
                    }
                }
            }
            _discardLocationOnEmpty = discardLocationOnEmpty;
        }
        
        public SearchAction(List<ItemSpawningDataRuntime> runtimeItems, bool discardLocationOnEmpty = false) {
            _lootTables = null;
            _itemsInsideContainer = runtimeItems;
            _discardLocationOnEmpty = discardLocationOnEmpty;
        }

        public static SearchAction JsonCreate() => new SearchAction();

        // === Initialization
        protected override void OnInitialize() {
            if (_lootTables != null) {
                foreach (var lootTable in _lootTables.WhereNotNull()) {
                    AddItemsFromLootTable(lootTable);
                }

                _lootTables = null;
            }
            
            MergeItemsData();

            ParentModel.AfterFullyInitialized(AddListeners);
        }
        
        public bool IsEmpty() {
            if (ParentModel == null) {
                return true;
            }
            bool isEmpty = _itemsInsideContainer.Count <= 0;
            
            IInventory characterItems = RetrieveInventory();
            if (characterItems != null) {
                isEmpty = isEmpty && !characterItems.AllItemsVisibleOnUI().Any();
            }
            if (_backupInventory != null) {
                isEmpty = isEmpty && !_backupInventory.AllItemsVisibleOnUI().Any();
            }
            
            return isEmpty;
        }

        void AddItemsFromLootTable(ILootTable lootTable) {
            try {
                _itemsInsideContainer.AddRange(lootTable.PopLoot(this).items);
            } catch (Exception e) {
                Log.Important?.Error($"Exception below happened on popping loot from SearchAction of LocationTemplate ({ParentModel.Spec.GetLocationId()})", ParentModel.Spec);
                Debug.LogException(e, ParentModel.Spec);
            }
        }
        
        /// <summary>
        /// Merges items with the same template, sums their quantity
        /// </summary>
        void MergeItemsData() {
            var itemsGrouped = _itemsInsideContainer
                .GroupBy(i => new { i.ItemTemplate, i.itemLvl })
                .ToList();

            var newList = new List<ItemSpawningDataRuntime>();

            foreach (var grouping in itemsGrouped) {
                if (grouping.Key?.ItemTemplate == null) continue;

                if (grouping.Key.ItemTemplate.CanStack) {
                    int summedQuantity = grouping.Sum(i => i.quantity);
                    var item = new ItemSpawningDataRuntime(grouping.Key.ItemTemplate) {
                        quantity = summedQuantity,
                        itemLvl = grouping.Key.itemLvl
                    };
                    newList.Add(item);
                } else {
                    foreach (var item in grouping) {
                        newList.Add(item);
                    }
                }
            }

            _itemsInsideContainer = newList;
        }

        protected override void OnRestore() {
            ParentModel.AfterFullyInitialized(AddListeners);
        }
        
        void AddListeners() {
            NpcElement npcElement = ParentModel.TryGetElement<NpcElement>();
            // --- we can only search an NPC that has died
            if (npcElement is { IsAlive: true }) {
                _corpseLoot = npcElement.Template.CorpseLoot;
                _wyrdConvertedLoot = npcElement.Template.WyrdConvertedLoot;
                SearchAvailable = false;
                npcElement.ListenTo(IAlive.Events.BeforeDeath, _ => {
                    EnableSearchAfterDelay().Forget();
                }, this);
            }
        }

        async UniTaskVoid EnableSearchAfterDelay() {
            if (!await AsyncUtil.DelayTime(this, 0.75f)) {
                return;
            }

            if (_corpseLoot != null) {
                foreach (var lootTable in _corpseLoot.WhereNotNull()) {
                    AddItemsFromLootTable(lootTable);
                }
            }

            if (_wyrdConvertedLoot != null && World.Services.Get<WyrdnessService>().IsWyrdNight) {
                foreach (var lootTable in _wyrdConvertedLoot.WhereNotNull()) {
                    AddItemsFromLootTable(lootTable);
                }
            }
            
            SetSearchAvailable(true);
        }
        
        // === Persistence

        public override void Serialize(SaveWriter writer) {
            base.Serialize(writer);
            if (_itemsInsideContainer != null) {
                writer.WriteName(SavedFields._itemsInsideContainer);
                writer.WriteList(_itemsInsideContainer, static (writer, item) => writer.Write(item));
                writer.WriteSeparator();
            }
            if (_backupInventory != null) {
                writer.WriteName(SavedFields._backupInventory);
                writer.WriteModel(_backupInventory);
                writer.WriteSeparator();
            }
            if (_discardLocationOnEmpty) {
                writer.WriteName(SavedFields._discardLocationOnEmpty);
                writer.Write(_discardLocationOnEmpty);
                writer.WriteSeparator();
            }
        }

        // === Public interface

        public bool HasUnlockedItemsToShow() {
            if (_itemsInsideContainer.Count > 0 && _itemsInsideContainer.Any(i => !i.ItemTemplate.HiddenOnUI)) {
                return true;
            }
            
            IInventory characterItems = RetrieveInventory();
            if (characterItems != null) {
                if (characterItems.AllUnlockedAndVisibleItems().Any()) {
                    return true;
                }
            }
            if (_backupInventory != null) {
                if (_backupInventory.AllUnlockedAndVisibleItems().Any()) {
                    return true;
                }
            }
            return false;
        }
        
        public override ActionAvailability GetAvailability(Hero hero, IInteractableWithHero interactable) {
            return SearchAvailable ? base.GetAvailability(hero, interactable) : ActionAvailability.Disabled;
        }

        public override IHeroInteractionUI InteractionUIToShow(IInteractableWithHero interactable) =>
            new AutoSearchHeroInteractionUI(interactable, this);

        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (interactable is not Location location || IsEmpty()) return;
            World.All<ContainerUI>().Where(c => c.ParentModel != location).ToArray().ForEach(c => c.Discard());
            if (!location.HasElement<ContainerUI>()) {
                ShowContainerContents(location);
            }
        }

        protected override void OnEnd(Hero hero, IInteractableWithHero interactable) {
            ParentModel.TryGetElement<ContainerUI>()?.Discard();
        }

        public void SetSearchAvailable(bool available) {
            SearchAvailable = available;
        }

        public bool Contains([NotNull] ItemTemplate template) {
            return _itemsInsideContainer.Any(item => item.ItemTemplate == template) 
                   || RetrieveInventory().Items.Any(item => item.Template == template);
        }

        public void GetAllItems(List<ItemSpawningDataRuntime> listToFill) {
            foreach (var item in RetrieveInventory().Items) {
                listToFill.Add(new ItemSpawningDataRuntime(item));
            }
            listToFill.AddRange(_itemsInsideContainer);
        }

        public void DropAllItemsAndDiscard(bool onlyImportant) {
            List<ItemSpawningDataRuntime> items = new();
            GetAllItems(items);
            foreach (var itemSpawningDataRuntime in items) {
                if (onlyImportant && !itemSpawningDataRuntime.ItemTemplate.IsImportantItem) {
                    continue;
                }
                if (itemSpawningDataRuntime.ItemTemplate is not { HiddenOnUI: false }) {
                    continue;
                }
                DroppedItemSpawner.SpawnDroppedItemPrefab(ParentModel.Coords + Vector3.up * 0.25f, itemSpawningDataRuntime.ItemTemplate, 
                    itemSpawningDataRuntime.quantity, itemSpawningDataRuntime.itemLvl, itemSpawningDataRuntime.weightLvl, Random.rotation);
            }
            Discard();
        }

        public void MoveItem(IInventory inventory, ItemTemplate template, int quantity) {
            var currentInventory = RetrieveInventory();
            GetNItems(template, quantity, out var itemsFromContainer, out var itemsFromInventory);

            foreach (var itemData in itemsFromContainer) {
                // We add it to inventory for correct move item behaviour (and not creating a new item in the target inventory)
                Item newItem = currentInventory.Add(new Item(itemData));
                newItem.MoveTo(inventory);
                _itemsInsideContainer.Remove(itemData);
            }

            foreach (var item in itemsFromInventory) {
                item.MoveTo(inventory);
            }
        }
        
        public void MoveItem(IInventory inventory, ItemTemplate template) {
            var currentInventory = RetrieveInventory();
            
            foreach (var itemData in _itemsInsideContainer.Where(i => i.ItemTemplate == template).ToArray()) {
                // We add it to inventory for correct move item behaviour (and not creating a new item in the target inventory)
                var newItem = currentInventory.Add(new Item(itemData));
                newItem.MoveTo(inventory);
                _itemsInsideContainer.Remove(itemData);
            }
            
            foreach (var item in inventory.GetSimilarItemsInInventory(i => i.Template == template)) {
                item.MoveTo(inventory);
            }
        }
        
        public void RemoveItem(ItemTemplate template) {
            _itemsInsideContainer.RemoveAll(i => i.ItemTemplate == template);
            RetrieveInventory().Items.Where(i => i.Template == template).ToArray().ForEach(i => i.Discard());
        }

        void GetNItems(ItemTemplate template, int quantity, out List<ItemSpawningDataRuntime> itemsFromContainer, out List<Item> itemsFromInventory) {
            itemsFromContainer = new List<ItemSpawningDataRuntime>();
            itemsFromInventory = new List<Item>();
            foreach (var itemData in _itemsInsideContainer.Where(i => i.ItemTemplate == template).ToArray()) {
                if (itemData.quantity > quantity) {
                    itemData.quantity -= quantity;
                    itemsFromContainer.Add(new ItemSpawningDataRuntime(itemData.ItemTemplate, quantity, itemData.itemLvl));
                    return;
                } else {
                    quantity -= itemData.quantity;
                    itemsFromContainer.Add(itemData);
                    if (quantity <= 0) {
                        return;
                    }
                }
            }
            
            itemsFromInventory.AddRange(RetrieveInventory().GetSimilarItemsInInventory(i => i.Template == template, quantity));
        }

        public ContainerUI ShowContainerContents(Location location, bool openTransfer = false) {
            var ui = new ContainerUI(RetrieveInventory(), _itemsInsideContainer, _discardLocationOnEmpty);
            location.AddElement(ui);
            if (openTransfer) {
                ui.TransferItems();
            }

            return ui;
        }
        
        IInventory RetrieveInventory() {
            IInventory result = ParentModel.Inventory;
            if (result == null) {
                result = _backupInventory ??= AddElement(new ContainerInventory());
            } else if (_backupInventory != null) {
                foreach (Item item in _backupInventory.Items.ToList()) {
                    item.MoveTo(result);
                }

                _backupInventory.Discard();
                _backupInventory = null;
            }

            return result;
        }
    }
}