using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Heroes.Housing.Farming {
    [Serializable]
    public partial struct PlantedSeedData {
        public ushort TypeForSerialization => SavedTypes.PlantedSeedData;

        [Saved] public ItemTemplate seedTemplate;
        [Saved] public string plantName;
        [Saved] public PlantStage[] stages;
        [Saved] public ItemSpawningData resultItem;
        [Saved] public ARTimeSpan totalGrowthTime;

        public PlantedSeedData(ItemSeed itemSeed) {
            seedTemplate = itemSeed.SeedItem.Template;
            resultItem = itemSeed.resultItem;
            plantName = resultItem.ItemTemplate(itemSeed).ItemName;
            stages = itemSeed.stages;
            totalGrowthTime = itemSeed.totalGrowthTime;
        }
    }
}