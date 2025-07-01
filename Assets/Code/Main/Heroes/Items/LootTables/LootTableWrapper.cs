using System;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootTableWrapper {
        [SerializeField]
        LootType lootType = LootType.Explicit;

        [SerializeField, ShowIf(nameof(ShowLootTableAsset))]
        [TemplateType(typeof(LootTableAsset))]
        TemplateReference lootReference;

        [SerializeReference, ShowIf(nameof(ShowEmbedLoot))]
        ILootTable table;

        public LootType Type => lootType;
        public LootTableAsset LootTableAsset(object debugTarget) => lootReference.Get<LootTableAsset>(debugTarget);

        public ILootTable EmbedTable => table;
        public ILootTable LootTable(object debugTarget) => lootType == LootType.Explicit ? LootTableAsset(debugTarget)?.table : table;

        // === Helpers
        bool ShowLootTableAsset => lootType == LootType.Explicit;
        bool ShowEmbedLoot => lootType == LootType.Embed;

        public enum LootType {
            Explicit = 0,
            Embed = 1
        }
    }
}