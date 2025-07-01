using System.Collections.Generic;
using Awaken.TG.Main.Character;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Items.Loadouts {
    public interface ILoadout : IElement {
        ICharacterInventory Inventory { get; }
        Item this[EquipmentSlotType slotType] { get; }
        bool IsEquipped { get; }

        bool IsSlotLocked(EquipmentSlotType slot);
        /// <summary>
        /// Assigns item to loadout and tries to equip it if loadout is active
        /// </summary>
        void EquipItem(EquipmentSlotType slot, Item item);
        
        /// <summary>
        /// Store item-slot pair in cache, doesn't equip items, used for internal purposes
        /// </summary>
        void InternalAssignItem(EquipmentSlotType slot, Item item);
        
        /// <summary>
        /// Make loadout active (equip it's items) 
        /// </summary>
        void Activate();
    }

    public static class LoadoutExtensions {
        /// <summary>
        /// Uses internal functions that prevent triggering infinite loops
        /// The only, uniform place to actually equip items from loadout onto Hero
        /// </summary>
        public static void EquipLoadoutItems(this ILoadout loadout) {
            if (!loadout.IsEquipped) return;
            
            foreach (var slot in EquipmentSlotType.Loadouts) {
                Item previous = loadout.Inventory.EquippedItem(slot);
                if (previous != null) {
                    loadout.Inventory.UnequipInternal(previous, null);
                }
            }

            HashSet<Item> equippedItems = new();
            
            foreach (var slot in EquipmentSlotType.Loadouts) {
                Item item = loadout[slot];
                if (item != null) {
                    loadout.Inventory.EquipSlotInternal(slot, item);
                    equippedItems.Add(item);
                }
            }

            foreach (var item in equippedItems) {
                loadout.Inventory.EquipItemInternal(item);
            }
        }
    }
}