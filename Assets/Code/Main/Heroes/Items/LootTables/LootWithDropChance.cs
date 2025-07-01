using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootWithDropChance : ILootTable {
        [SerializeReference, InlineProperty, HideLabel, BoxGroup("box", showLabel: false)]
        public ILootTable loot;
        [Space]
        [Range(0f, 1f), BoxGroup("box", showLabel: false)] 
        public float chance;
        [BoxGroup("box", showLabel: false)]
        public bool useLootChanceMultiplier = true;
        float ChanceMultiplier => World.Any<Hero>()?.HeroStats.LootChanceMultiplier.ModifiedValue ?? 1;
        
        public LootTableResult PopLoot(object debugTarget) {
            if (loot == null) {
                return new LootTableResult();
            }

            float calculatedChance = chance;
            if (useLootChanceMultiplier) {
                calculatedChance *= ChanceMultiplier;
            }
            if (Random.value < calculatedChance) {
                return loot.PopLoot(debugTarget);
            } else {
                return new LootTableResult();
            }
        }

        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            if (loot == null) {
                yield break;
            }

            foreach (var item in loot.EDITOR_PopLootData()) {
                item.probability *= chance;
                item.AffectedByLootChanceMultiplier = useLootChanceMultiplier;
                yield return item;
            }
        }
    }
}