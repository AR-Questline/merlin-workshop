using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.Tools;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootArrayPerHeroLevel : ILootTable {
        [SerializeReference, InlineProperty]
        public LootTableWithLevel[] array = Array.Empty<LootTableWithLevel>();

        public LootTableResult PopLoot(object debugTarget) {
            if (array == null || array.Length == 0) {
                return new LootTableResult();
            }

            int heroLevel = Hero.Current.Level.ModifiedInt;
            int closestLevel = int.MinValue;
            ILootTable selectedLootTable = null;
            foreach (var tableWithLevel in array) {
                if (tableWithLevel.availableFromLevel <= heroLevel && tableWithLevel.availableFromLevel > closestLevel) {
                    closestLevel = tableWithLevel.availableFromLevel;
                    selectedLootTable = tableWithLevel.lootTable;
                }
            }
            
            return selectedLootTable != null ? selectedLootTable.PopLoot(debugTarget) : new LootTableResult();
        }

        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            if (array == null || array.Length == 0) {
                yield break;
            }
            
            foreach (var lootTable in array) {
                if (lootTable.lootTable == null) {
                    continue;
                }
                foreach (var item in lootTable.lootTable.EDITOR_PopLootData()) {
                    yield return item;
                }
            }
        }

        [Serializable]
        public class LootTableWithLevel {
            public int availableFromLevel;
            [SerializeReference, InlineProperty]
            public ILootTable lootTable;
        }
    }
}