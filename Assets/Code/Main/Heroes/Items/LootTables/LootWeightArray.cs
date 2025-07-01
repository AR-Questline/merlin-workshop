using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Heroes.Items.Tools;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootWeightArray : ILootTable {
        [LabelWidth(150), BoxGroup("box", showLabel: false)]
        public int amount = 1;
        [LabelWidth(150), LabelText("Randomly Add Up To"), BoxGroup("box", showLabel: false)]
        public int randomizedAmount = 0;
        [BoxGroup("box", showLabel: false)]
        public LootWithWeight[] loots;

        [Serializable]
        public class LootWithWeight {
            [SerializeReference, InlineProperty, HideLabel]
            public ILootTable loot;
            [Space, LabelWidth(60)]
            public int weight = 1;
        }

        public LootTableResult PopLoot(object debugTarget) {
            if (loots == null || !loots.Any()) return new LootTableResult();
            var currentLoots = loots.ToList();
            var realAmount =
                amount + UnityEngine.Random.Range(0,
                    randomizedAmount +
                    1); // +1 because Random.Range for int values has exclusive max value, e.g. for Range(0, 3) possible values are 0, 1 and 2, but not 3
            var result = new LootTableResult();
            for (int i = 0; i < realAmount; i++) {
                if (!currentLoots.Any()) break;
                var selected = RandomUtil.WeightedSelect(currentLoots, s => s.weight);
                currentLoots.Remove(selected);
                if (selected == null) continue;
                result = result.Merge(selected.loot.PopLoot(debugTarget));
            }

            return result;
        }

        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            if (loots == null || !loots.Any()) {
                yield break;
            }

            float weightSum = loots.Sum(l => l.weight);
            int minCount = amount;
            int maxCount = amount + randomizedAmount;
            int avgCount = (minCount + maxCount) / 2;

            foreach (var lootWithWeight in loots) {
                // Bold approximation
                float chanceNotToDrop = Mathf.Pow(1 - lootWithWeight.weight / weightSum, avgCount);
                float chanceToDrop = 1 - chanceNotToDrop;
                foreach (var item in lootWithWeight.loot.EDITOR_PopLootData()) {
                    item.probability *= chanceToDrop;
                    yield return item;
                }
            }
        }
    }
}