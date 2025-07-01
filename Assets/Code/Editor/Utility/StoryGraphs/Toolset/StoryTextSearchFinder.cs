using System;
using System.Linq;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using XNode;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all usages of given text in story graphs\n" +
                 "1. Provide any part of dialogue text (case-insensitive)\n" +
                 "2. Click Execute button\n" +
                 "3. You can check the full text content from the result entry")]
    public class StoryTextSearchFinder : StoryGraphUtilityTool<SearchResult<StoryTextSearchResultEntry>, StoryTextSearchResultEntry> {
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField, Required] 
        string text;
        
        protected override bool Validate() {
            return !string.IsNullOrEmpty(text);
        }

        protected override void ExecuteTool() {
            ResultController.SetCurrentlySearched(text);

            var elementTrios = GraphConverterUtils.AllElementsWithInterface<StoryNode, IStoryTextRef>()
                .Where(trio => !trio.node.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.StoryTextSearch)
                               && ((IStoryTextRef)trio.element).Text.Translate().Contains(text, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(trio => trio.graph.name);

            foreach (var trio in elementTrios) {
                string elementText = ((IStoryTextRef)trio.element).Text.Translate();
                string note = $"Owner is {trio.element.GetType().Name}";
                ResultController.Feed(new StoryTextSearchResultEntry(trio.graph, trio.node, elementText, note));
            }
        }
    }
    
    [Serializable]
    public class StoryTextSearchResultEntry : DefaultResultEntry {
        [SerializeField, ReadOnly] string text;

        public StoryTextSearchResultEntry(NodeGraph graph, StoryNode node, string storyText, string notes = "") : base(graph, node, notes) {
            text = storyText;
        }
    }
}
