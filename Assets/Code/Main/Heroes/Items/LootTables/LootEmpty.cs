using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.Tools;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootEmpty : ILootTable {
        public LootTableResult PopLoot(object debugTarget) {
            return new LootTableResult();
        }

        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            yield break;
        }
    }
}