using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootTableAssetRef : ILootTable {

        [SerializeField, TemplateType(typeof(LootTableAsset)), BoxGroup("box", showLabel: false), HideLabel]
        TemplateReference table;

        public TemplateReference Table => table;
        
        public LootTableResult PopLoot(object debugTarget) {
            if (table == null || !table.IsSet) {
                return new LootTableResult();
            }
            return table.Get<LootTableAsset>().PopLoot();
        }
        
        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            if (table == null || !table.IsSet) {
                yield break;
            }

            foreach (var item in table.Get<LootTableAsset>().EDITOR_PopLootData()) {
                yield return item;
            }
        }
    }
}