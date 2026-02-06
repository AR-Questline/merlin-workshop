using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.NewGamePlus;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootLevelOverride : ILootTable {
        [LabelWidth(60)]
        public int lvl = 0;
        public bool grantBonusLevelFromNgPlus = true;

        [SerializeReference, InlineProperty, LabelWidth(60), BoxGroup("box", showLabel: false), HideLabel]
        public ILootTable loot;

        public LootTableResult PopLoot(object debugTarget) {
            if (loot == null) {
                return new LootTableResult();
            }
            LootTableResult result = loot.PopLoot(debugTarget);
            
            int targetLevel = grantBonusLevelFromNgPlus 
                ? lvl + NewGamePlusSystem.CalculatedBonusItemLevel
                : lvl;
            foreach (ItemSpawningDataRuntime item in result.items) {
                item.itemLvl = targetLevel;
            }

            return result;
        }

        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            if (loot == null) {
                yield break;
            }

            foreach (var item in loot.EDITOR_PopLootData()) {
                yield return item;
            }
        }
    }
}