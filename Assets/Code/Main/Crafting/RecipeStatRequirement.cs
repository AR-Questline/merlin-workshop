using System;
using Awaken.TG.Main.General.StatTypes;

namespace Awaken.TG.Main.Crafting {
    [Serializable]
    public struct RecipeStatRequirement {
        public HeroStatType statType;
        public int value;

        public RecipeStatRequirement(HeroStatType statType, int value) {
            this.statType = statType;
            this.value = value;
        }
    }
}