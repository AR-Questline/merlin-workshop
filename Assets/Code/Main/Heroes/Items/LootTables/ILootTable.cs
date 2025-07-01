using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.Tools;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    public interface ILootTable {
        LootTableResult PopLoot(object debugTarget);
        IEnumerable<ItemLootData> EDITOR_PopLootData();
    }
}