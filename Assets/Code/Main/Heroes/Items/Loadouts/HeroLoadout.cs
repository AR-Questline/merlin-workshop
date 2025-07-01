using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items.Loadouts {
    public partial class HeroLoadout : Element<HeroItems>, ILoadout {
        public override ushort TypeForSerialization => SavedModels.HeroLoadout;

        public const int Count = 5;
        public const int HiddenLoadoutIndex = 4;

        [Saved] public int LoadoutIndex { get; private set; }
        [Saved] public bool IsEquipped { get; private set; }
        [Saved] List<SlotAndItem> _cache = new List<SlotAndItem>(EquipmentSlotType.All.Length);

        public ICharacterInventory Inventory => ParentModel;
        Hero Hero => ParentModel.ParentModel;

        public bool IsRanged => PrimaryItem?.IsRanged ?? false;
        public Item PrimaryItem => GetItemInternal(EquipmentSlotType.MainHand);
        public Item SecondaryItem => GetItemInternal(IsRanged ? EquipmentSlotType.Quiver : EquipmentSlotType.OffHand);
        public bool Contains(Item item) => PrimaryItem == item || SecondaryItem == item;

        public Item this[EquipmentSlotType slot] => GetItemInternal(slot);
        Item GetItemInternal(EquipmentSlotType slot) => _cache.FirstOrDefault(p => p.slot == slot).item;

        public new static class Events {
            public static readonly Event<HeroItems, Change<int>> LoadoutChanged = new(nameof(LoadoutChanged));
            public static readonly Event<HeroItems, Item> FailedToChangeLoadout = new(nameof(FailedToChangeLoadout));
            public static readonly Event<HeroLoadout, LoadoutItemChange> ItemInLoadoutChanged = new(nameof(ItemInLoadoutChanged));
        }

        [JsonConstructor, UnityEngine.Scripting.Preserve] HeroLoadout() { }

        public HeroLoadout(int index) {
            LoadoutIndex = index;
        }

        protected override void OnInitialize() {
            Hero.ListenTo(IItemOwner.Relations.Owns.Events.AfterDetached, OnItemDropped, this);
            Hero.AfterFullyInitialized(InitializeFists);
            ParentModel.ListenTo(ICharacterInventory.Events.AfterEquipmentChanged, CheckForReEquipFists, this);
        }

        protected override void OnRestore() {
            for (int i = 0; i < _cache.Count; i++) {
                var (slot, item) = _cache[i];
                if (item is not { HasBeenDiscarded: false } || Array.IndexOf(EquipmentSlotType.Loadouts, slot) == -1) {
                    EquipItem(slot, null);
                }
            }
            OnInitialize();
        }

        public void Activate() {
            if (IsEquipped) return;

            foreach (var loadout in ParentModel.Elements<HeroLoadout>()) {
                loadout.IsEquipped = false;
            }

            IsEquipped = true;
            using (new PostponeEquipmentChange(ParentModel)) {
                this.EquipLoadoutItems();
            }
        }

        public void EquipItem(EquipmentSlotType slot, Item item) {
            if (!CanEquipItem(item, slot)) {
                return;
            }
            
            if (item == null) {
                if (IsEquipped) {
                    ParentModel.Unequip(slot, this);
                } else {
                    // TODO: No idea how to do it properly right now (unequip 2h weapon, etc)
                    Item previous = GetItemInternal(slot);
                    foreach (var s in EquipmentSlotType.Loadouts) {
                        if (GetItemInternal(s) == previous) {
                            InternalAssignItem(s, null);
                        }
                    }
                }
            } else {
                ParentModel.Equip(item, slot, this);
                if (!IsEquipped) {
                    item.TryGetElement<ItemEquip>()?.PlayEquipToggleSound(Hero, true);
                }
            }
        }

        public void Unequip(Item item) {
            foreach (var slot in EquipmentSlotType.Loadouts) {
                if (GetItemInternal(slot) == item) {
                    EquipItem(slot, null);
                }
            }
        }

        public void InternalAssignItem(EquipmentSlotType slot, Item item) {
            var existingIndex = _cache.FindIndex(p => p.slot == slot);
            var previousItem = existingIndex == -1 ? null : _cache[existingIndex].item;
            if (existingIndex == -1) {
                if (item) {
                    _cache.Add(new SlotAndItem(slot, item));
                }
            } else {
                if (item) {
                    _cache[existingIndex] = new SlotAndItem(slot, item);
                } else {
                    _cache.RemoveAt(existingIndex);
                }
            }
            this.Trigger(Events.ItemInLoadoutChanged, new LoadoutItemChange(this, slot, previousItem, item));
        }

        void OnItemDropped(RelationEventData evtData) {
            if (evtData.to is Item item) {
                foreach (var slot in EquipmentSlotType.Loadouts) {
                    if (GetItemInternal(slot) == item) {
                        InternalAssignItem(slot, null);
                    }
                }
            }
        }
        
        // === Fists
        void InitializeFists() {
            if (this[EquipmentSlotType.MainHand] == null) {
                ParentModel.Equip(ParentModel.GetMainHandFist(), EquipmentSlotType.MainHand, this);
            }
            if (this[EquipmentSlotType.OffHand] == null) {
                ParentModel.Equip(ParentModel.GetOffHandFist(), EquipmentSlotType.OffHand, this);
            }
        }

        void CheckForReEquipFists() {
            if (this[EquipmentSlotType.MainHand] == null) {
                ParentModel.Equip(ParentModel.GetMainHandFist(), EquipmentSlotType.MainHand, this);
            }
            
            if (this[EquipmentSlotType.OffHand] == null) {
                ParentModel.Equip(ParentModel.GetOffHandFist(), EquipmentSlotType.OffHand, this);
            }
        }
        
        // === Helpers
        public bool IsSlotLocked(EquipmentSlotType slot) {
            return Elements<HeroLoadoutSlotLocker>().Any(l => l.SlotTypeLocked == slot);
        }
        
        public bool TryGetSlotOfItem(Item item, out EquipmentSlotType slotType) {
            var existingIndex = _cache.FindIndex(p => p.item == item);
            if (existingIndex == -1) {
                slotType = null;
                return false;
            } else {
                slotType = _cache[existingIndex].slot;
                return true;
            }
        }
        
        public bool CanEquipItem(Item item, EquipmentSlotType slot = null) {
            bool slotLocked = IsSlotLocked(slot);
            bool loadoutCondition = item != null && !EquipmentSlotType.HeroLoadoutCondition(item, this);
            if (slotLocked || loadoutCondition) {
                ParentModel.Trigger(Events.FailedToChangeLoadout, item);
                return false;
            }
            return true;
        }

        public struct LoadoutItemChange {
            public HeroLoadout loadout;
            public EquipmentSlotType slot;
            public Item from;
            public Item to;
            
            public LoadoutItemChange(HeroLoadout loadout, EquipmentSlotType slot, Item from, Item to) {
                this.loadout = loadout;
                this.slot = slot;
                this.from = from;
                this.to = to;
            }
        }

        public partial struct SlotAndItem {
            public ushort TypeForSerialization => SavedTypes.SlotAndItem;

            [Saved] public EquipmentSlotType slot;
            [Saved] public Item item;

            public SlotAndItem(EquipmentSlotType slot, Item item) {
                this.slot = slot;
                this.item = item;
            }

            public void Deconstruct(out EquipmentSlotType slot, out Item item) {
                slot = this.slot;
                item = this.item;
            }
        }
    }
}