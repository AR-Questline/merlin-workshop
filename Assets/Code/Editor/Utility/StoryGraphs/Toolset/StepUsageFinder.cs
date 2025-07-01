using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all usages of a step type\n" +
                "1. Select desired step type from dropdown\n" +
                "2. Click Execute button\n" +
                "3. Find desired result entry")]
    public class StepUsageFinder : StoryGraphUtilityTool<SearchResult<DefaultResultEntry>, DefaultResultEntry> {
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [Required, TypeDrawerSettings(BaseType = typeof(NodeElement)), ShowInInspector] 
        Type _stepType;
        [BoxGroup(InputSectionName), PropertyOrder(InputSectionOrder + 1)]
        [SerializeField]
        bool showHidden;
        
        protected override bool Validate() => _stepType != null;
        
        protected override void ExecuteTool() {
            ResultController.SetCurrentlySearched(_stepType.Name);
            IEnumerable<NodeGraph> graphs = CollectAllStoriesWithDesiredStep()
                                            .Where(g => showHidden || !((StoryGraph) g).hiddenInToolWindows.HasFlagFast(EditorFinderType.StepUsage))
                                            .OrderBy(graph => graph.name);
            ResultController.Feed(PrepareResults(graphs));
        }   

        IEnumerable<NodeGraph> CollectAllStoriesWithDesiredStep() {
            return (IEnumerable<NodeGraph>)typeof(GraphConverterUtils)
                .GetMethod(nameof(AllStoriesWithElement),
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                ?.MakeGenericMethod(_stepType)
                .Invoke(null, null);
        }
        
        List<DefaultResultEntry> PrepareResults(IEnumerable<NodeGraph> graphs) {
            return (graphs ?? Array.Empty<NodeGraph>())
                .SelectMany(graph => graph.nodes.OfType<StoryNode>())
                .SelectMany(node => node.NodeElements)
                .Where(node => node.GetType() == _stepType)
                .Select(step => new DefaultResultEntry(step.genericParent.graph, step.genericParent))
                .ToList();
        }
    }
}
