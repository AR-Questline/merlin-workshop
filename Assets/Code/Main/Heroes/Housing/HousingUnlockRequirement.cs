using System;
using System.Text;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Templates;

namespace Awaken.TG.Main.Heroes.Housing {
    [Serializable]
    public struct HousingUnlockRequirement {
        public int unlockPrice;
        public ItemSpawningData[] requiredResources;
        public ItemSpawningData requiredFurnitureItem;

        public HousingUnlockRequirement(ItemSpawningData itemSpawningData) {
            unlockPrice = 0;
            requiredResources = Array.Empty<ItemSpawningData>();
            requiredFurnitureItem = itemSpawningData;
        }

        public string GetRequirementsDescription() {
            StringBuilder requirements = new();
            requirements.Append($"{unlockPrice} ");
            foreach (ItemSpawningData item in requiredResources) {
                requirements.AppendLine($"{item.ItemTemplate(null).ItemName} x{item.quantity} ");
            }
            return requirements.ToString();
        }
    }
}