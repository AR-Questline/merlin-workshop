using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.LootTables;
using Awaken.TG.Main.Stories.Conditions;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all usage of desired Item\n" +
                 "1. Provide or Clear Item Reference\n" +
                 "2. Select additional conditions\n" +
                 "3. Click Execute button\n" +
                 "4. You can check all usages and all used items in 2 lists below")]
    public class ItemUsageFinder : StoryGraphUtilityTool<SearchResult<ItemUsageStepResultEntry>, ItemUsageStepResultEntry> {
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField] bool SearchHasItem = true;
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField] bool SearchGiveItem = true;
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField] bool SearchTakeItem = true;
        
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField, TemplateType(typeof(ItemTemplate))] TemplateReference requiredTemplate;
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField] Condition dropable;
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField] Condition importantItem;
        
        [field: BoxGroup(ResultSectionName, centerLabel: true), HideLabel, PropertyOrder(ResultSectionOrder)]
        [field: SerializeField, TableList(IsReadOnly = true, AlwaysExpanded = true, DefaultMinColumnWidth = 180, ShowPaging = true, NumberOfItemsPerPage = 100), Searchable(Recursive = false, FilterOptions = SearchFilterOptions.ISearchFilterableInterface)]
        List<ItemResultEntry> Results2 { get; set; } = new();
        
        protected override bool Validate() {
            return true;
        }
        
        protected override void ExecuteTool() {
            var itemToFind = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(requiredTemplate.GUID));
            ResultController.SetCurrentlySearched($"Item {itemToFind?.name} Usage");

            HashSet<GameObject> itemGOs = new HashSet<GameObject>();
            
            if (SearchHasItem) {
                var allHasItems = AllElements<StoryNode, CEditorHasItems>()
                    .Select(trio => (trio.graph, trio.node, trio.element));

                foreach (var valueTuple in allHasItems.OrderBy(tuple => tuple.graph.name)) {
                    foreach (var item in valueTuple.element.requiredItemTemplateReferenceQuantityPairs) {
                        if (ConditionValid(valueTuple.graph, item)) {
                            string path = AssetDatabase.GUIDToAssetPath(item.itemTemplateReference.GUID);
                            GameObject go = null;
                            if (!string.IsNullOrEmpty(path)) {
                                go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            }
                            ResultController.Feed(new ItemUsageStepResultEntry(valueTuple.graph, valueTuple.node, valueTuple.element.name, go));
                            itemGOs.Add(go);
                        }
                    }
                }
            }

            if (SearchGiveItem || SearchTakeItem) {
                var allChangeItems = AllElements<StoryNode, SEditorChangeItemsQuantity>()
                    .Select(trio => (trio.graph, trio.node, trio.element));

                foreach (var valueTuple in allChangeItems.OrderBy(tuple => tuple.graph.name)) {
                    foreach (var item in valueTuple.element.itemTemplateReferenceQuantityPairs) {
                        if (item.quantity > 0 && !SearchGiveItem) {
                            continue;
                        }
                        if (item.quantity < 0 && !SearchTakeItem) {
                            continue;
                        }
                        if (ConditionValid(valueTuple.graph, item)) {
                            string path = AssetDatabase.GUIDToAssetPath(item.itemTemplateReference.GUID);
                            GameObject go = null;
                            if (!string.IsNullOrEmpty(path)) {
                                go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            }
                            ResultController.Feed(new ItemUsageStepResultEntry(valueTuple.graph, valueTuple.node, valueTuple.element.name, go));
                            itemGOs.Add(go);
                        }
                    }
                }
            }

            Results2.Clear();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Found Item Usages:");
            foreach (var go in itemGOs) {
                Results2.Add(new ItemResultEntry(go));
                sb.AppendLine(go?.name);
            }
            Log.Important?.Error(sb.ToString());
        }

        bool ConditionValid(NodeGraph graph, ItemSpawningData item) {
            if (item.itemTemplateReference is not { IsSet: true }) {
                return false;
            }
            
            if (requiredTemplate is { IsSet: true }) {
                if (requiredTemplate.GUID != item.itemTemplateReference.GUID) {
                    return false;
                }
            }
            
            var template = item.itemTemplateReference.Get<ItemTemplate>();
            if (template == null) {
                Log.Critical?.Error($"Item template is null despite reference being set {item.itemTemplateReference.GUID} {graph}", graph);
                return false;
            }
            
            if (dropable == Condition.HasToBe) {
                if (template.cannotBeDropped) {
                    return false;
                }
            } else if (dropable == Condition.CantBe) {
                if (!template.cannotBeDropped) {
                    return false;
                }
            }
            
            if (importantItem == Condition.HasToBe) {
                if (!template.IsImportantItem) {
                    return false;
                }
            } else if (importantItem == Condition.CantBe) {
                if (template.IsImportantItem) {
                    return false;
                }
            }
            
            return true;
        }
        
        [Serializable]
        internal enum Condition : byte {
            Ignore,
            HasToBe,
            CantBe,
        } 
    }
    
    [Serializable]
    public class ItemUsageStepResultEntry : DefaultResultEntry {
        [SerializeField, ReadOnly] string usageName;
        [SerializeField, ReadOnly] string itemName;
        [SerializeField, ReadOnly] GameObject gameObject;

        public ItemUsageStepResultEntry(NodeGraph graph, StoryNode node, string stepName, GameObject itemGO, string notes = "") : base(graph, node, notes){
            usageName = stepName;
            gameObject = itemGO;
            itemName = itemGO?.name;
        }
    }
    
    [Serializable]
    public class ItemResultEntry {
        [SerializeField, ReadOnly] string itemName;
        [SerializeField, ReadOnly] GameObject gameObject;

        public ItemResultEntry(GameObject itemGO) {
            gameObject = itemGO;
            itemName = itemGO?.name;
        }
    }
}
