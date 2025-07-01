using System.Linq;
using Awaken.TG.Main.Heroes.Housing.Farming;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Heroes.Items.Attachments {
    public partial class ItemSeed : Element<Item>, IRefreshedByAttachment<ItemSeedAttachment> {
        public override ushort TypeForSerialization => SavedModels.ItemSeed;

        public PlantSize plantSize;
        public PlantStage[] stages;
        public ItemSpawningData resultItem;
        public ARTimeSpan totalGrowthTime;

        public Item SeedItem => ParentModel;

        public void InitFromAttachment(ItemSeedAttachment spec, bool isRestored) {
            plantSize = spec.PlantSize;
            resultItem = spec.ItemReference;
            stages = spec.Stages;
            totalGrowthTime = new ARTimeSpan(stages.Sum(stage => stage.growthTime.Ticks));
        }
    }
}