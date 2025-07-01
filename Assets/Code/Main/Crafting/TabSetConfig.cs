using System.Collections.Generic;
using Awaken.TG.Main.Crafting.Cooking;

namespace Awaken.TG.Main.Crafting {
    public struct TabSetConfig {
        public TabSetConfig(Dictionary<CraftingTabTypes, CraftingTemplate> dictionary) {
            Dictionary = dictionary;
        }
        public Dictionary<CraftingTabTypes, CraftingTemplate> Dictionary { get; }
    }
}