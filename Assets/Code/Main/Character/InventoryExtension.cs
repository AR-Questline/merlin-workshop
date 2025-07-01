using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Skills;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Tags;

namespace Awaken.TG.Main.Character {
    public static class InventoryExtension {
        
        public static float NumberOfItems(this IInventory inventory, ItemTemplate itemTemplate) {
            return inventory.Items.Where(i => i.Template == itemTemplate).Sum(i => i.Quantity);
        }

        public static float NumberOfStolenItems(this IInventory inventory, ItemTemplate itemTemplate) {
            return inventory.Items.Where(i => i.IsStolen && i.Template == itemTemplate).Sum(i => i.Quantity);
        }

        [UnityEngine.Scripting.Preserve]
        public static float NumberOfItemsWithTags(this IInventory inventory, ICollection<string> tags) {
            return inventory.Items.Where(i => TagUtils.HasRequiredTags(i, tags)).Sum(i => i.Quantity);
        }

        [UnityEngine.Scripting.Preserve]
        public static IEnumerable<Skill> AllSkills(this IInventory inventory) {
            return inventory.Items.SelectMany(i => i.ActiveSkills);
        }
    }
}