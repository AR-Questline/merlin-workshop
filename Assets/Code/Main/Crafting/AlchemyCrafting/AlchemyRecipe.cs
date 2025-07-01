using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Scenes.SceneConstructors;

namespace Awaken.TG.Main.Crafting.AlchemyCrafting {
    public class AlchemyRecipe : BaseRecipe {
        public override ProfStatType ProficiencyStat => ProfStatType.Alchemy;
        public override HeroStatType BonusLevelStat => HeroStatType.AlchemyLevelBonus;
        protected override ItemTemplate GarbageItem => CommonReferences.Get.AlchemyGarbageItemTemplate;
    }
}