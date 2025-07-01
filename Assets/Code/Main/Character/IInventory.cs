using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Character {
    public interface IInventory : IModel {
        /// <summary>
        /// All items in this inventory irrespective of sub states
        /// </summary>
        IEnumerable<Item> Items { get; }
        
        Item Add(Item item, bool allowStacking = true);
        
        /// <summary>
        /// Removes item relations ignoring stacking
        /// </summary>
        void Remove(Item item, bool discard = true);
        bool CanBeTheft { get; }
    }

    public static class InventoryExtensions {
        public static IEnumerable<Item> AllItemsVisibleOnUI(this IInventory inventory) {
            return inventory.Items.Where(item => !item.HiddenOnUI);
        }
        
        public static IEnumerable<Item> AllUnlockedAndVisibleItems(this IInventory inventory) {
            bool anyMeleeWeaponLocked = false;
            bool anyRangedWeaponLocked = false;
            return inventory.Items.Where(item => !item.HiddenOnUI && !ShouldBeLocked(item, ref anyMeleeWeaponLocked, ref anyRangedWeaponLocked));
        }
        
        [UnityEngine.Scripting.Preserve]
        public static IEnumerable<Item> AllUnlockedItems(this IInventory inventory) {
            bool anyMeleeWeaponLocked = false;
            bool anyRangedWeaponLocked = false;
            return inventory.Items.Where(item => !ShouldBeLocked(item, ref anyMeleeWeaponLocked, ref anyRangedWeaponLocked));
        }
        
        public static IEnumerable<Item> AllLockedItems(this IInventory inventory) {
            bool anyMeleeWeaponLocked = false;
            bool anyRangedWeaponLocked = false;
            return inventory.Items.Where(item => ShouldBeLocked(item, ref anyMeleeWeaponLocked, ref anyRangedWeaponLocked));
        }
        
        static bool ShouldBeLocked(Item item, ref bool anyMeleeWeaponLocked, ref bool anyRangedWeaponLocked) {
            if (item.IsArmor && item.IsEquipped) {
                return true;
            }

            if (!anyMeleeWeaponLocked && item.IsWeapon && item.IsMelee) {
                anyMeleeWeaponLocked = true;
                return true;
            }

            if (!anyRangedWeaponLocked && item.IsWeapon && item.IsRanged) {
                anyRangedWeaponLocked = true;
                return true;
            }

            return false;
        }
    }
}