using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.Memories;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootTableFlagConditional : ILootTable {
        [Tags(TagsCategory.Flag), BoxGroup("Flag", showLabel: false), HideLabel]
        public string flag = "";

        [SerializeReference, LabelWidth(100), InlineProperty, BoxGroup("Flag", showLabel: false)]
        public ILootTable ifTrue;
        [SerializeReference, LabelWidth(100), InlineProperty, BoxGroup("Flag", showLabel: false)]
        public ILootTable ifFalse;
        
        public LootTableResult PopLoot(object debugTarget) {
            var result = World.Services.Get<GameplayMemory>().Context().Get<bool>(flag) ? ifTrue : ifFalse;
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