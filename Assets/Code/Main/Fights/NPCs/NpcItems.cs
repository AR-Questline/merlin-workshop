using Awaken.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Fights.NPCs {
    public sealed partial class NpcItems : Element<Location>, ICharacterInventory, ILoadout, IWithDomainMovedCallback {
        public override ushort TypeForSerialization => SavedModels.NpcItems;

        [Saved] ItemInSlots _itemInSlots;

        int ICharacterInventory.EquippingSemaphore { get; set; }
        Dictionary<EquipmentSlotType, Item> _loadoutCache = new();
        
        public IItemOwner Owner => ParentModel;
        public ICharacterInventory Inventory => this;
        public bool IsEquipped => true;
        public bool CanBeTheft => true;

        public IEnumerable<Item> Items => OwnedItems;
        public IEnumerable<Item> AllWeapons => OwnedItems.Where(i => i.IsWeapon && !i.IsMagic);
        RelatedList<Item> OwnedItems => Owner.RelatedList(IItemOwner.Relations.Owns);
        RelatedList<Item> ContainedItems => RelatedList(ICharacterInventory.Relations.Contains);

        public bool AllowEquipping => true;
        public ref readonly ItemInSlots ItemInSlots => ref _itemInSlots;

        protected override void OnInitialize() {
            _itemInSlots.Init(this);

            _loadoutCache = new Dictionary<EquipmentSlotType, Item>();
            foreach (var slot in EquipmentSlotType.Loadouts) {
                _loadoutCache[slot] = null;
            }

            this.ListenTo(Events.AfterFullyInitialized, AfterInit, this);
        }

        protected override void OnRestore() {
            _itemInSlots.Init(this);

            _loadoutCache = new Dictionary<EquipmentSlotType, Item>();
            foreach (var slot in EquipmentSlotType.Loadouts) {
                _loadoutCache[slot] = this.EquippedItem(slot);
            }
            
            this.ListenTo(Events.AfterFullyInitialized, AfterInit, this);
        }

        void AfterInit() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<Item>(), this, model => RemoveFromInventory((Item)model));
            this.ListenTo(ICharacterInventory.Relations.Contains.Events.Changed, TriggerChange, this);
            Owner.ListenTo(IItemOwner.Relations.Owns.Events.AfterDetached, OnItemDropped, this);
            Owner.ListenTo(Events.BeforeDiscarded, _ => DiscardAllOwnedItems(), this);
        }

        public Item Add(Item item, bool allowStacking = true) {
            if (!item.IsInitialized) {
                World.Add(item);
            }
            
            // check if item is already owned by npc
            if (OwnedItems.Contains(item)) return item;
            
            var hookResult = ICharacterInventory.Events.ItemToBeAddedToInventory.RunHooks(item, new ICharacterInventory.AddingItemInfo(this, item));
            if (hookResult.Prevented) {
                return item;
            }
            item = hookResult.Value.Item;
            
            this.Trigger(ICharacterInventory.Events.BeforePickedUpItem, item);

            if (allowStacking && Items.TryStackItem(item, out var stackedTo)) {
                // successfully stacked item
                this.Trigger(ICharacterInventory.Events.PickedUpItem, stackedTo);
                return stackedTo;
            }
            
            if (item.Inventory != null) {
                throw new Exception("Item still has a reference to its old inventory. This is not allowed!");
            }

            if (!OwnedItems.Add(item)) {
                return item;
            }
            AddNewItemToInventory(item);

            return item;
        }

        public void Remove(Item item, bool discard = true) {
            if (item.IsEquipped) {
                this.Unequip(item);
            }
            
            RemoveFromInventory(item);
            
            // check if item is owned by npc
            if (OwnedItems.Contains(item)) {
                OwnedItems.Remove(item);
            }
            
            if (discard) {
                item.Discard();
            }
        }

        void ICharacterInventory.EquipSlotInternal(EquipmentSlotType slot, Item item) {
            this._itemInSlots.Equip(slot, item);
        }
        void ICharacterInventory.EquipItemInternal(Item item) { }

        void ICharacterInventory.UnequipSlotInternal(EquipmentSlotType slot) {
            this._itemInSlots.Unequip(slot);
        }
        void ICharacterInventory.UnequipItemInternal(Item item) { }
        
        public void DomainMoved(Domain newDomain) {
            foreach (var item in OwnedItems) {
                item.MoveToDomain(newDomain);
            }
        }
        
        // === Loadout
        public ILoadout CurrentLoadout => this;

        public Item this[EquipmentSlotType slot] => _loadoutCache[slot];

        public bool IsSlotLocked(EquipmentSlotType slot) => true;
        public void EquipItem(EquipmentSlotType slot, Item item) {
            if (item == null) {
                this.Unequip(slot);
            } else {
                this.Equip(item, slot, this);
            }
        }
        
        public void InternalAssignItem(EquipmentSlotType slot, Item item) {
            _loadoutCache[slot] = item;
        }

        public void Activate() { }

        // === Helpers
        void AddNewItemToInventory(Item item) {
            if (!ContainedItems.Contains(item)) {
                ContainedItems.Add(item);
            }
            
            this.Trigger(ICharacterInventory.Events.PickedUpNewItem, item);
            this.Trigger(ICharacterInventory.Events.PickedUpItem, item);
        }
        
        void RemoveFromInventory(Item item) {
            if (!ContainedItems.Contains(item)) {
                return;
            }
            ContainedItems.Remove(item);

            World.EventSystem.RemoveAllListenersBetween(item, this);
        }

        void OnItemDropped(RelationEventData evtData) {
            if (evtData.to is Item item) {
                foreach (var slot in EquipmentSlotType.Loadouts) {
                    if (_loadoutCache[slot] == item) {
                        InternalAssignItem(slot, null);
                    }
                }
            }
        }

        void DiscardAllOwnedItems() {
            if (Owner.WasDiscardedFromDomainDrop) return;
            
            var items = OwnedItems.ToList();
            for (int i = 0; i < items.Count; i++) {
                items[i].Discard();
            }
        }

        // === Discard
        protected override void OnDiscard(bool fromDomainDrop) {
            _itemInSlots.Teardown();
        }
    }
}