using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Locations.Containers;
using Awaken.TG.Utility;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Times;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions {
    public partial class FurnitureSearchAction : AbstractLocationAction, IRefreshedByAttachment<FurnitureSearchAttachment> {
        public override ushort TypeForSerialization => SavedModels.FurnitureSearchAction;

        LootTableWrapper _lootTableWrapper;
        List<ItemSpawningData> _additionalItems;
        object _debugTarget;

        ILootTable _lootTable;
        
        public ARTimeSpan RenewLootRate { get; private set; }
        public List<ItemSpawningDataRuntime> ItemsInsideContainer { get; private set; }
        
        public override InfoFrame ActionFrame => IsEmpty
            ? new InfoFrame($"{LocTerms.Search.Translate()} ({LocTerms.Empty.Translate()})", false)
            : InfoFrame.Empty;
        
        public bool IsEmpty => ItemsInsideContainer == null || ItemsInsideContainer.Count == 0;
        
        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public FurnitureSearchAction() { }
        
        public void InitFromAttachment(FurnitureSearchAttachment spec, bool isRestored) {
            _lootTableWrapper = spec.lootTableWrapper;
            _additionalItems = spec.additionalItems;
            RenewLootRate = spec.renewLootRate;
        }
        
        public override IHeroInteractionUI InteractionUIToShow(IInteractableWithHero interactable) =>
            new AutoFurnitureSearchHeroInteractionUI(interactable, this);
        
        protected override void OnStart(Hero hero, IInteractableWithHero interactable) {
            if (interactable is not Location location || IsEmpty) return;
            if (!location.HasElement<ContainerUI>()) {
                var ui = new ContainerUI(RetrieveInventory(), ItemsInsideContainer, false);
                location.AddElement(ui);
            }
        }
        
        IInventory RetrieveInventory() {
            return AddElement(new ContainerInventory());
        }
        
        protected override void OnEnd(Hero hero, IInteractableWithHero interactable) {
            ParentModel.TryGetElement<ContainerUI>()?.Discard();
        }

        public void GenerateLoot() {
            GenerateLoot(_lootTableWrapper, _additionalItems);
        }

        public void GenerateLoot(List<ItemSpawningDataRuntime> itemsInsideContainer) {
            ItemsInsideContainer = itemsInsideContainer;
        }

        void GenerateLoot(LootTableWrapper lootTableWrapper, List<ItemSpawningData> additionalItems) {
            _lootTableWrapper = lootTableWrapper;
            _additionalItems = additionalItems ?? new List<ItemSpawningData>();
            _lootTable = _lootTableWrapper.LootTable(_debugTarget);
            ItemsInsideContainer = new List<ItemSpawningDataRuntime>();
            
            var validItems = _additionalItems.Where(itemSpawningData => itemSpawningData.itemTemplateReference is {IsSet:false}).ToList();

            foreach (ItemSpawningData itemSpawningData in validItems) {
                ItemsInsideContainer.Add(itemSpawningData.ToRuntimeData(_debugTarget));
            }

            if (_lootTable != null) {
                AddItemsFromLootTable(_lootTable);
            }
        }
        
        void AddItemsFromLootTable(ILootTable lootTable) {
            try {
                ItemsInsideContainer.AddRange(lootTable.PopLoot(this).items);
            } catch (Exception e) {
                Log.Important?.Error($"Exception below happened on popping loot from SearchAction of LocationTemplate ({ParentModel.Spec.GetLocationId()})", ParentModel.Spec);
                Debug.LogException(e, ParentModel.Spec);
            }
        }
    }
}