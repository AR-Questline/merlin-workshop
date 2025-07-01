using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items.Tools;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootArray : ILootTable {
        [SerializeReference, InlineProperty]
        public ILootTable[] array = Array.Empty<ILootTable>();

        public LootTableResult PopLoot(object debugTarget) {
            if (array == null || array.Length == 0) {
                return new LootTableResult();
            }
            return array.Select(l => l != null ? l.PopLoot(debugTarget) : new LootTableResult())
                .Aggregate((a, b) => a.Merge(b));
        }

        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            if (array == null || array.Length == 0) {
                yield break;
            }
            
            foreach (var lootTable in array) {
                if (lootTable == null) {
                    continue;
                }
                foreach (var item in lootTable.EDITOR_PopLootData()) {
                    yield return item;
                }
            }
        }
    }
}