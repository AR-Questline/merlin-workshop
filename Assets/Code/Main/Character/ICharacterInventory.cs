using System;
using System.Collections.Generic;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.Loadouts;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.TG.MVC.Relations;
using Awaken.Utility.Debugging;
using JetBrains.Annotations;

namespace Awaken.TG.Main.Character {
    public interface ICharacterInventory : IInventory {
        internal int EquippingSemaphore { get; set; }
        
        IItemOwner Owner { get; }
        ILoadout CurrentLoadout { get; }
        bool AllowEquipping { get; }

        ref readonly ItemInSlots ItemInSlots { get; }

        [UsedImplicitly, UnityEngine.Scripting.Preserve] bool IsUnarmored {
            get {
                foreach (var itemInSlot in ItemInSlots.EquippedItems()) {
                    if (itemInSlot.IsArmor) {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Called when equipping item <br/>
        /// (For every slot it is equipped in) <br/>
        /// </summary>
        internal void EquipSlotInternal(EquipmentSlotType slot, Item item);
        
        /// <summary>
        /// Called when equipping item <br/>
        /// (Only once - no matter how many slots) <br/>
        /// </summary>
        internal void EquipItemInternal(Item item);
        
        /// <summary>
        /// Called when unequipping item <br/>
        /// (For every slot it is equipped in) <br/>
        /// </summary>
        internal void UnequipSlotInternal(EquipmentSlotType slot);

        /// <summary>
        /// Called when unequipping item. <br/>
        /// (Only once - no matter how many slots) <br/>
        /// </summary>
        internal void UnequipItemInternal(Item item);

        public static class Relations {
            static readonly RelationPair<ICharacterInventory, Item> Containment = new(typeof(Relations), Arity.One, nameof(Contains), Arity.Many, nameof(ContainedBy), IItemOwner.Relations.Ownership);

            /// <summary>
            /// Items that lie in inventory (not equipped)
            /// </summary>
            public static readonly Relation<ICharacterInventory, Item> Contains = Containment.LeftToRight;
            public static readonly Relation<Item, ICharacterInventory> ContainedBy = Containment.RightToLeft;
        }

        public static class Events {
            public static readonly Event<ICharacterInventory, ICharacterInventory> AfterEquipmentChanged = new(nameof(AfterEquipmentChanged));
            public static readonly Event<ICharacterInventory, Item> PickedUpNewItem = new(nameof(PickedUpNewItem));
            public static readonly HookableEvent<IModel, AddingItemInfo> ItemToBeAddedToInventory = new(nameof(ItemToBeAddedToInventory));
            public static readonly Event<ICharacterInventory, Item> BeforePickedUpItem = new(nameof(BeforePickedUpItem));
            public static readonly Event<ICharacterInventory, Item> PickedUpItem = new(nameof(PickedUpItem));
            /// <summary>
            /// Currently Hero only. 
            /// </summary>
            public static readonly Event<ICharacterInventory, DroppedItemData> ItemDropped = new(nameof(ItemDropped));
            // -- Slots
            public static readonly Event<ICharacterInventory, EquipmentSlotType> AnySlotChanged = new(nameof(AnySlotChanged));
            public static Event<ICharacterInventory, Item> SlotEquipped(EquipmentSlotType slot) => slot.SlotEquipped;
            public static Event<ICharacterInventory, Item> SlotUnequipped(EquipmentSlotType slot) => slot.SlotUnequipped;
            public static Event<ICharacterInventory, ICharacterInventory> SlotChanged(EquipmentSlotType slot) => slot.SlotChanged;
        }

        public class AddingItemInfo {
            public ICharacterInventory Inventory { get; }
            public Item Item { get; set; }

            public AddingItemInfo(ICharacterInventory inventory, Item item) {
                Inventory = inventory;
                Item = item;
            }
        }
    }

    public static class CharacterInventoryExtension {
        // --- Retrieving Items

        public static Item EquippedItem(this ICharacterInventory inventory, EquipmentSlotType slot) {
            return inventory.ItemInSlots[slot.Index];
        }

        /// <summary> Items that are equipped </summary>
        public static ItemInSlots.EquippedItemsEnumerator EquippedItems(this ICharacterInventory inventory) {
            return inventory.ItemInSlots.EquippedItems();
        }

        public static ItemInSlots.DistinctEquippedItemsEnumerator DistinctEquippedItems(this ICharacterInventory inventory) {
            return inventory.ItemInSlots.DistinctEquippedItems();
        }

        /// <summary> Items that lay in inventory but are not equipped </summary>
        public static IEnumerable<Item> ContainedItems(this ICharacterInventory inventory) {
            return inventory.RelatedList(ICharacterInventory.Relations.Contains);
        }

        public static bool IsEquipped(this ICharacterInventory inventory, EquipmentSlotType slot) {
            return inventory.ItemInSlots[slot.Index];
        }

        public static EquipmentSlotType SlotWith(this ICharacterInventory inventory, Item item) {
            return inventory.ItemInSlots.SlotWith(item);
        }

        // --- Equipping

        public static bool Equip(this ICharacterInventory inventory, Item item, EquipmentSlotType slot = null, ILoadout loadout = null) {
            if (!inventory.AllowEquipping) {
                return false;
            }
            
            var equip = item.TryGetElement<ItemEquip>();
            if (equip != null) {
                slot ??= equip.GetBestEquipmentSlotType();
                return inventory.Equip(item, equip, slot, loadout);
            }
            Log.Important?.Error($"Cannot equip item {LogUtils.GetDebugName(item)}. It is not equippable");
            return false;
        }

        static bool Equip(this ICharacterInventory inventory, Item item, ItemEquip equip, EquipmentSlotType slotType, ILoadout loadout) {
            if (IsLoadoutItem(item)) {
                loadout ??= inventory.CurrentLoadout;
                if (loadout[slotType] == item) {
                    return false;
                }
            } else if (inventory.EquippedItem(slotType) == item) {
                return false;
            }
            
            using (new PostponeEquipmentChange(inventory)) {
                equip.EquipmentType.ResolveEquipping(item, inventory, loadout, slotType);
                if (loadout != null) {
                    loadout.EquipLoadoutItems();
                } else {
                    inventory.EquipItemInternal(item);
                }
            }
            return true;
        }

        // --- Unequipping

        public static Item Unequip(this ICharacterInventory inventory, EquipmentSlotType slot, ILoadout loadout = null) {
            var item = inventory.EquippedItem(slot);
            if (item != null) {
                inventory.Unequip(item, loadout);
            }

            return item;
        }
        
        public static void Unequip(this ICharacterInventory inventory, Item item, ILoadout loadout = null) {
            if (!inventory.AllowEquipping) {
                return;
            }
            
            if (IsLoadoutItem(item)) {
                loadout ??= inventory.CurrentLoadout;
            }
            UnequipInternal(inventory, item, loadout);
            loadout?.EquipLoadoutItems();
        }

        public static void UnequipInternal(this ICharacterInventory inventory, Item item, ILoadout loadout) {
            var slotTypes = item.EquippedInSlotOfTypes;
            using (new PostponeEquipmentChange(inventory)) {
                for (int i = slotTypes.Count - 1; i >= 0; i--) {
                    loadout?.InternalAssignItem(slotTypes[i], null);
                    inventory.UnequipSlotInternal(slotTypes[i]);
                }

                inventory.UnequipItemInternal(item);
            }
        }

        static bool IsLoadoutItem(Item item) => item.EquipmentType.Category is EquipmentCategory.Weapon or EquipmentCategory.Ammo;

        public static bool IsDualWielding(this ICharacterInventory inventory) {
            EquipmentType mainHandItemEquipmentType = inventory.EquippedItem(EquipmentSlotType.MainHand)?.EquipmentType;
            EquipmentType offHandItemEquipmentType = null;
            var offHandItem = inventory.EquippedItem(EquipmentSlotType.OffHand);
            if (offHandItem != null && offHandItem.Template != CommonReferences.Get.HandCutOffItemTemplate) {
                offHandItemEquipmentType = offHandItem.EquipmentType;
            }
            bool isMainHandBlocking = mainHandItemEquipmentType == EquipmentType.Shield || mainHandItemEquipmentType == EquipmentType.Rod;
            bool isOffHandFistOrMagic = offHandItemEquipmentType == EquipmentType.Fists || offHandItemEquipmentType == EquipmentType.Magic;
            bool isDualWieldingWithBlocking = isMainHandBlocking && offHandItemEquipmentType != null && !isOffHandFistOrMagic;
            bool isOffHandWeapon = offHandItemEquipmentType == EquipmentType.OneHanded && mainHandItemEquipmentType != EquipmentType.Magic;
            bool bothFists = mainHandItemEquipmentType == EquipmentType.Fists && offHandItemEquipmentType == EquipmentType.Fists;
            return isDualWieldingWithBlocking || isOffHandWeapon || bothFists;
        }
    }

    public struct PostponeEquipmentChange : IDisposable {
        ICharacterInventory _inventory;

        public PostponeEquipmentChange(ICharacterInventory inventory) {
            _inventory = inventory;
            _inventory.EquippingSemaphore++;
        }

        public void Dispose() {
            _inventory.EquippingSemaphore--;
            if (_inventory.EquippingSemaphore == 0) {
                _inventory.Trigger(ICharacterInventory.Events.AfterEquipmentChanged, _inventory);
            }
        }
    }
}