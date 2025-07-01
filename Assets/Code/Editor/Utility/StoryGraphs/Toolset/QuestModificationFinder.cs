using System;
using System.Linq;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Quests.Templates;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all type of quest modification\n" +
                 "1. Drag and drop or pick target Quest Template prefab\n" +
                 "2. Click Execute button\n" +
                 "3. You could check modification type in the results list")]
    public class QuestModificationFinder : StoryGraphUtilityTool<SearchResult<QuestModificationResultEntry>, QuestModificationResultEntry> {
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField, TemplateType(typeof(QuestTemplateBase))]
        TemplateReference searchedQuest;
        
        public QuestTemplateBase SearchedQuest => searchedQuest == null && string.IsNullOrEmpty(searchedQuest?.GUID) ? null : searchedQuest.Get<QuestTemplateBase>();
        
        protected override bool Validate() {
            return string.IsNullOrEmpty(searchedQuest?.GUID) || SearchedQuest != null;
        }
        
        protected override void ExecuteTool() {
            ResultController.SetCurrentlySearched(SearchedQuest.name);
            
            var allQuestModification = AllElementsWithInterface<StoryNode, IStoryQuestRef>()
                .Where(trio => !trio.node.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.QuestModification) 
                               && ((IStoryQuestRef)trio.element).QuestRef.Equals(searchedQuest))
                .Select(trio => (trio.graph, trio.node, trio.element.GetType().Name, ((IStoryQuestRef) trio.element).TargetValue));
            
            foreach (var valueTuple in allQuestModification.OrderBy(tuple => tuple.graph.name)) {
                ResultController.Feed(new QuestModificationResultEntry(valueTuple.graph, valueTuple.node, valueTuple.Name, valueTuple.TargetValue));
            } 
        }
    }
    
    [Serializable]
    public class QuestModificationResultEntry : DefaultResultEntry {
        [SerializeField, DisplayAsString, GUIColor(nameof(ModificationColor))] string modificationName;
        [SerializeField, DisplayAsString] string targetValue;

        public QuestModificationResultEntry(NodeGraph graph, StoryNode node, string stepName, string targetValue, string notes = "") : base(graph, node, notes){
            modificationName = stepName;
            this.targetValue = targetValue;
        }
        
        Color ModificationColor() {
            if (modificationName == null) {
                return Color.white;
            }
            if (modificationName.Contains("Fail")) return GUIColors.Red;
            if (modificationName.Contains("Complete")) return GUIColors.Green;
            if (modificationName.Contains("Add")) return GUIColors.BlueishHighlight;
            return Color.white;
        }
    }
}
