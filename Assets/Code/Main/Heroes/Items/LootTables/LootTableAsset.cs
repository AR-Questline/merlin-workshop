using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Items.Tools;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Items.LootTables {
    [Serializable]
    public class LootTableAsset : ScriptableObject, ITemplate {
        [SerializeReference, InlineProperty, HideLabel]
        public ILootTable table;
        
        [SerializeField, HideInInspector] TemplateMetadata metadata;

        public TemplateMetadata Metadata => metadata;
        
        public LootTableResult PopLoot() {
            if(table == null) return new LootTableResult();
            return table.PopLoot(this);
        }

        public string GUID { get; set; }
        public PooledList<ITemplate> DirectAbstracts => PooledList<ITemplate>.Empty;
        public bool IsAbstract => false;
        
        string INamed.DisplayName => string.Empty;
        string INamed.DebugName => name;
        
        // === Editor
        public IEnumerable<ItemLootData> EDITOR_PopLootData() {
            if (table == null) {
                yield break;
            }

            foreach (var item in table.EDITOR_PopLootData()) {
                yield return item;
            }
        }
        
#if UNITY_EDITOR
        const string FoldoutGroupName = "ADD BY TAGS";
        [Header(FoldoutGroupName), SerializeField, Tags(TagsCategory.Item)] 
        string[] addItemsWithTags = Array.Empty<string>();

        [Button]
        void Add() {
            bool areYouSure = true;
            if (table != null) {
                areYouSure = UnityEditor.EditorUtility.DisplayDialog("Replace", "You will replace current table, are you sure?", "Yes", "No");
            }
            if (!areYouSure) return;
            
            var templatesProvider = new TemplatesProvider();
            templatesProvider.StartLoading();

            List<ItemTemplate> matchingItems = new();
            
            foreach (var item in templatesProvider.GetAllOfType<ItemTemplate>()) {
                if (!item.IsAbstract && TagUtils.HasRequiredTags(item, addItemsWithTags)) {
                    matchingItems.Add(item);
                }
            }

            LootWeightArray array = new();
            array.loots = new LootWeightArray.LootWithWeight[matchingItems.Count];
            for (int i = 0; i < matchingItems.Count; i++) {
                ItemTemplate item = matchingItems[i];
                ItemSpawningData spawningData = new(new TemplateReference(item));
                array.loots[i] = new LootWeightArray.LootWithWeight {
                    loot = spawningData,
                    weight = 1,
                };
            }

            table = array;
        }
#endif
    }
}