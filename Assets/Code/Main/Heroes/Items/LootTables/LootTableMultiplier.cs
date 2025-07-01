using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items.Tools;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootTableMultiplier : ILootTable {
        [LabelWidth(150), BoxGroup("box", showLabel: false)]
        public int amount = 1;
        [LabelWidth(150), LabelText("Max Random Amount"), Tooltip("Random number between 0 and this value will be added to the amount"), BoxGroup("box", showLabel: false)]
        public int randomizedAmount = 0;

        [LabelWidth(150), Tooltip("Generate only one, if you want to have the same loot duplicated. Otherwise, the loot will be generated multiple times, possibly giving different results."), BoxGroup("box", showLabel: false)]
        public bool generateOnlyOnce = false;

        [Space]
        [SerializeReference, InlineProperty, HideLabel, BoxGroup("box", showLabel: false)]
        public ILootTable loot;

        public LootTableResult PopLoot(object debugTarget) {
            if (loot == null) {
                return new LootTableResult();
            }

            var realAmount =
                amount + UnityEngine.Random.Range(0,
                    randomizedAmount +
                    1); // +1 because Random.Range for int values has exclusive max value, e.g. for Range(0, 3) possible values are 0, 1 and 2, but not 3

            if (realAmount <= 0) {
                return new LootTableResult();
            }
            
            if (generateOnlyOnce) {
                return loot.PopLoot(debugTarget).Multiply(realAmount);
            } else {
                return Enumerable.Repeat(loot, realAmount).Select(l => l.PopLoot(debugTarget))
                    .Aggregate((a, b) => a.Merge(b));
            }
        }

        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            if (loot == null) {
                yield break;
            }

            int minMulti = amount;
            int maxMulti = amount + randomizedAmount;

            if (generateOnlyOnce) {
                foreach (var item in loot.EDITOR_PopLootData()) {
                    item.minCount *= minMulti;
                    item.maxCount *= maxMulti;
                    yield return item;
                }
            } else {
                for (int i = 0; i < maxMulti; i++) {
                    float probability = (maxMulti - i) / (float)(maxMulti - minMulti + 1);
                    probability = Math.Min(1f, probability);
                    foreach (var item in loot.EDITOR_PopLootData()) {
                        item.probability *= probability;
                        yield return item;
                    }
                }
            }
        }
    }
}