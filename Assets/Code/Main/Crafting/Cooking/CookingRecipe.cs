using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;

namespace Awaken.TG.Main.Crafting.Cooking {
    public class CookingRecipe : BaseRecipe {
        public override ProfStatType ProficiencyStat => ProfStatType.Cooking;
        public override HeroStatType BonusLevelStat => HeroStatType.CookingLevelBonus;
        protected override ItemTemplate GarbageItem => null;
    }
}