using Awaken.TG.Main.Crafting.Recipes;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Scenes.SceneConstructors;
using UnityEngine;

namespace Awaken.TG.Main.Crafting.HandCrafting {
    public class HandcraftingRecipe : BaseRecipe {
        public override ProfStatType ProficiencyStat => ProfStatType.Handcrafting;
        public override HeroStatType BonusLevelStat => HeroStatType.EquipmentLevelBonus;
        protected override ItemTemplate GarbageItem => CommonReferences.Get.HandcraftingGarbageItemTemplate;
    }
}