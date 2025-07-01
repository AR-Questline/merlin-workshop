using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.SceneCaches.Locations;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all usages of Flag\n" +
                 "1. Provide desired flag name (even part of it)\n" +
                 "2. Click Execute button\n" +
                 "3. You can copy the full flag name from the result entry")]
    public class FlagUsageFinder : StoryGraphUtilityTool<SearchResult<FlagResultEntry>, FlagResultEntry> {
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField, Required] 
        string flag;
        
        protected override bool Validate() {
            return !string.IsNullOrEmpty(flag);
        }

        protected override void ExecuteTool() {
            ResultController.SetCurrentlySearched(flag);
           
            foreach (var valueTuple in GatherAllFlagChange()) {
                ResultController.Feed(new FlagResultEntry(valueTuple.graph, valueTuple.node, valueTuple.flag, valueTuple.stepName, valueTuple.targetValue));
            }
        }

        IEnumerable<(NodeGraph graph, StoryNode node, string flag, string stepName, string targetValue)> GatherAllFlagChange() {
           return AllElementsWithInterface<StoryNode, IStoryFlagRef>()
               .Where(trio => !trio.node.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.FlagUsage) 
                              && ((IStoryFlagRef)trio.element).FlagRef.Contains(flag))
               .Select(trio => (trio.graph, trio.node, ((IStoryFlagRef)trio.element).FlagRef, trio.element.GetType().Name, ((IStoryFlagRef)trio.element).TargetValue))
               .Union(AllElements<StoryNode, SEditorLocationChangeInteractability>()
                   .Where(trio => !trio.node.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.FlagUsage)
                                  && trio.element.locationReference.tags.Any(t => t.Contains(flag)))
                   .Select(trio => (trio.graph, trio.node, FlagRef: string.Join(" | ", trio.element.locationReference.tags), trio.element.GetType().Name, ((LocationInteractability) trio.element.targetInteractability).EnumName)))
               .OrderByDescending(valueTuple => valueTuple.FlagRef.StartsWith(flag))
               .ThenBy(valueTuple => valueTuple.FlagRef);
        }
        
        [Button, PropertyOrder(InputSectionOrder)]
        void OpenLocationFinder() {
            LocationSearchWindow.ShowWindow();
        }
    }
    
    [Serializable]
    public class FlagResultEntry : DefaultResultEntry {
        [SerializeField, DisplayAsString] string foundFlag;
        [SerializeField, DisplayAsString] string usageName;
        [SerializeField, ReadOnly, TableColumnWidth(10), GUIColor(nameof(TargetValueColor))] string targetValue;
        
        public FlagResultEntry(NodeGraph graph, StoryNode node, string flag, string stepName, string targetValue) : base(graph, node){
            foundFlag = flag;
            usageName = stepName;
            this.targetValue = targetValue;
        }
        
        Color TargetValueColor {
            get {
                if (targetValue == "True")
                    return GUIColors.Green;
                else if (targetValue == "False")
                    return GUIColors.Red;
                return Color.white;
            }
        }
    }
}
