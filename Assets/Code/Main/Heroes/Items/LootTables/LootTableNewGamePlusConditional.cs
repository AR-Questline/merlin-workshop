using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.NewGamePlus;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootTableNewGamePlusConditional : ILootTable {
        public int minimumNewGamePlusLevel = 1;

        [SerializeReference, LabelWidth(100), InlineProperty, BoxGroup("Flag", showLabel: false)]
        public ILootTable ifTrue;
        [SerializeReference, LabelWidth(100), InlineProperty, BoxGroup("Flag", showLabel: false)]
        public ILootTable ifFalse;
        
        public LootTableResult PopLoot(object debugTarget) {
            var result = NewGamePlusSystem.Level >= minimumNewGamePlusLevel ? ifTrue : ifFalse;
            return result?.PopLoot(debugTarget) ?? new LootTableResult();
        }
        
        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            foreach (var item in ifTrue?.EDITOR_PopLootData() ?? Enumerable.Empty<ItemLootData>()) {
                item.Conditional = true;
                yield return item;
            }
            foreach (var item in ifFalse?.EDITOR_PopLootData() ?? Enumerable.Empty<ItemLootData>()) {
                item.Conditional = true;
                yield return item;
            }
        }
    }
}