using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes.Tags;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootItemWithTags : ILootTable {
        [SerializeField, Tags(TagsCategory.Item), BoxGroup("box", showLabel: false)] 
        string[] tags = Array.Empty<string>();
        [SerializeField, LabelWidth(100), BoxGroup("box", showLabel: false)] 
        int count = 1;
        
        public LootTableResult PopLoot(object debugTarget) {
            var items = World.Services.Get<TemplatesProvider>().GetAllOfType<ItemTemplate>()
                .Where(item => TagUtils.HasRequiredTags(item.Tags, tags))
                .ToArray();
            if (items.Length == 0 || count == 0) {
                return new LootTableResult(Array.Empty<ItemSpawningDataRuntime>());
            } else if (count == 1) {
                return new LootTableResult(new[] { ToRuntimeData(RandomUtil.UniformSelect(items)) });
            } else {
                return new LootTableResult(RandomUtil.UniformSelectMultiple(items, count).Select(ToRuntimeData));
            }
        }
        
        static ItemSpawningDataRuntime ToRuntimeData(ItemTemplate item) {
            return new ItemSpawningDataRuntime(item) {
                quantity = 1,
                itemLvl = 0,
            };
        }

        static readonly List<ItemTemplate> EditorItemsCache = new(100);

        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            EditorItemsCache.Clear();
            var items = TemplatesProvider.EditorGetAllOfType<ItemTemplate>(TemplateTypeFlag.Regular)
                .Where(item => TagUtils.HasRequiredTags(item.EditorTagsNoRefresh, tags));
            EditorItemsCache.AddRange(items);
            
            if (EditorItemsCache.Count != 0 && count != 0) {
                int n = EditorItemsCache.Count;
                int k = count;
                float chance = M.NewtonBinomial(n - 1, k - 1) / (float)M.NewtonBinomial(n, k);
                foreach (var item in EditorItemsCache) {
                    yield return new ItemLootData(new TemplateReference(item), 1, chance);
                }
            }
            EditorItemsCache.Clear();
        }
    }
}