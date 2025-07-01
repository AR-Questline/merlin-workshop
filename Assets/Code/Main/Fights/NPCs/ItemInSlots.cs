using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Awaken.TG.Main.Fights.NPCs {
    public struct ItemInSlots : IListenerOwner {
        ICharacterInventory _owner;
        Item[] _itemsBySlot;
        IEventListener[] _listenersBySlot;

        public ItemInSlots(Item[] items) {
            _itemsBySlot = items;
            _listenersBySlot = new IEventListener[items.Length];
            _owner = null;
        }

        // === Queries
        public Item this[int index] => _itemsBySlot[index];
        public Item this[EquipmentSlotType slotType] => this[slotType.Index];
        [UnityEngine.Scripting.Preserve] public bool IsEquipped(Item item) => Array.IndexOf(_itemsBySlot, item) >= 0;
        public bool IsEquipped(EquipmentSlotType slot) => _itemsBySlot[slot.Index] != null;
        [UnityEngine.Scripting.Preserve] public bool IsEquipped(EquipmentSlotType slot, Item item) => _itemsBySlot[slot.Index] == item;
        public readonly EquipmentSlotType SlotWith(Item item) {
            var index = Array.IndexOf(_itemsBySlot, item);
            return index >= 0 ? EquipmentSlotType.All[index] : null;
        }

        // === Lifecycle
        public void Init(ICharacterInventory owner) {
            _owner = owner;

            if (_itemsBySlot == null) {
                _itemsBySlot = new Item[EquipmentSlotType.All.Length];
                _listenersBySlot = new IEventListener[EquipmentSlotType.All.Length];
            } else if (_itemsBySlot.Length != EquipmentSlotType.All.Length) {
                Log.Important?.Warning("For some reason, itemsBySlot length is not equal to EquipmentSlotType.All.Length. Resizing it. For owner: " + owner);
                Array.Resize(ref _itemsBySlot, EquipmentSlotType.All.Length);
                Array.Resize(ref _listenersBySlot, EquipmentSlotType.All.Length);
            }

            for (var i = 0; i < _itemsBySlot.Length; ++i) {
                var item = _itemsBySlot[i];
                if (item != null) {
                    var slotType = EquipmentSlotType.All[i];
                    var thisCopy = this;
                    _listenersBySlot[i] = item.ListenTo(Model.Events.BeforeDiscarded, _ => thisCopy.Unequip(slotType));
                }
            }
        }

        public void Teardown() {
            for (int i = 0; i < _listenersBySlot.Length; i++) {
                World.EventSystem.TryDisposeListener(ref _listenersBySlot[i]);
            }
        }

        // === Operations
        public void Equip(EquipmentSlotType slotType, Item item) {
            if (!slotType.Accept(item)) {
                throw new Exception($"Cannot put item {LogUtils.GetDebugName(item)} to slot {slotType}");
            }

            var oldItem = _itemsBySlot[slotType.Index];
            if (oldItem == item) {
                return;
            }

            var slotIndex = slotType.Index;
            if (oldItem != null) {
                _itemsBySlot[slotIndex] = null;
                _owner.Trigger(ICharacterInventory.Events.SlotUnequipped(slotType), oldItem);
                oldItem.UnequipInSlot(slotType);
                World.EventSystem.TryDisposeListener(ref _listenersBySlot[slotIndex]);
            }

            _itemsBySlot[slotIndex] = item;

            if (item != null) {
                var thisCopy = this;
                _listenersBySlot[slotIndex] = item.ListenTo(Model.Events.BeforeDiscarded, _ => thisCopy.Unequip(slotType));

                _owner.Trigger(ICharacterInventory.Events.SlotEquipped(slotType), item);
                item.EquipInSlot(slotType);
            }

            _owner.Trigger(ICharacterInventory.Events.SlotChanged(slotType), _owner);
            _owner.Trigger(ICharacterInventory.Events.AnySlotChanged, slotType);
        }

        public void Unequip(EquipmentSlotType slotType) {
            var oldItem = _itemsBySlot[slotType.Index];

            if (oldItem == null) {
                return;
            }

            var slotIndex = slotType.Index;
            World.EventSystem.TryDisposeListener(ref _listenersBySlot[slotIndex]);
            _itemsBySlot[slotIndex] = null;

            _owner.Trigger(ICharacterInventory.Events.SlotUnequipped(slotType), oldItem);
            oldItem.UnequipInSlot(slotType);

            _owner.Trigger(ICharacterInventory.Events.SlotChanged(slotType), _owner);
            _owner.Trigger(ICharacterInventory.Events.AnySlotChanged, slotType);
        }

        // === Enumerators
        public readonly EquippedItemsEnumerator EquippedItems() {
            return new EquippedItemsEnumerator(this);
        }

        public readonly DistinctEquippedItemsEnumerator DistinctEquippedItems() {
            return new DistinctEquippedItemsEnumerator(this);
        }

        public ref struct EquippedItemsEnumerator {
            ItemInSlots _itemInSlots;
            int _index;

            public Item Current => _itemInSlots._itemsBySlot[_index];

            public EquippedItemsEnumerator(ItemInSlots itemInSlots) {
                _itemInSlots = itemInSlots;
                _index = -1;
            }

            public EquippedItemsEnumerator GetEnumerator() {
                return this;
            }

            public bool MoveNext() {
                while (++_index < _itemInSlots._itemsBySlot.Length) {
                    if (_itemInSlots._itemsBySlot[_index] != null) {
                        return true;
                    }
                }
                return false;
            }

            [UnityEngine.Scripting.Preserve]
            public int Count() {
                int count = 0;
                for (var i = 0; i < _itemInSlots._itemsBySlot.Length; ++i) {
                    if (_itemInSlots._itemsBySlot[i] != null) {
                        ++count;
                    }
                }

                return count;
            }

            public Item FirstOrDefault(Func<Item, bool> predicate) {
                for (var i = 0; i < _itemInSlots._itemsBySlot.Length; ++i) {
                    var item = _itemInSlots._itemsBySlot[i];
                    if (item != null && predicate(item)) {
                        return item;
                    }
                }
                return null;
            }
        }

        public ref struct DistinctEquippedItemsEnumerator {
            ItemInSlots _itemInSlots;
            int _index;
            UnsafeList<int> _alreadyUsedItems;

            public Item Current => _itemInSlots._itemsBySlot[_index];

            public DistinctEquippedItemsEnumerator(ItemInSlots itemInSlots) {
                _itemInSlots = itemInSlots;
                _index = -1;
                _alreadyUsedItems = new UnsafeList<int>(itemInSlots._itemsBySlot.Length, ARAlloc.Temp);
            }

            public DistinctEquippedItemsEnumerator GetEnumerator() {
                return this;
            }

            [UnityEngine.Scripting.Preserve]
            public void Dispose() {
                _alreadyUsedItems.Dispose();
            }

            public bool MoveNext() {
                while (++_index < _itemInSlots._itemsBySlot.Length) {
                    if (_itemInSlots._itemsBySlot[_index] != null && !_alreadyUsedItems.Contains(Current.GetHashCode())) {
                        _alreadyUsedItems.Add(Current.GetHashCode());
                        return true;
                    }
                }
                return false;
            }
        }

        // === Serialization
        public struct SerializationAccess {
            public static ref Item[] ItemsBySlot(ref ItemInSlots itemInSlots) => ref itemInSlots._itemsBySlot;
        }
    }
}