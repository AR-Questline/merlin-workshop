using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility.Audio;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Helper tool for VO implementation." +
                 "Find all graphs with given actors\n" +
                 "1. Provide Actor reference\n" +
                 "2. Click Execute button\n" +
                 "3. You can check usage type in the results list" +
                 "4. Generate CSV Events or update VO for all graphs from the results list.")]
    public class MultiActorFinder : StoryGraphUtilityTool<SearchResult<DefaultResultEntry>, DefaultResultEntry> {
        
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField]
        List<ActorRef> searchActors = new();

        [BoxGroup(ResultSectionName, centerLabel :true), PropertyOrder(ResultSectionOrder)]
        [Button]
        public void GenerateVoiceOversCSV() {
            var graphs = ResultController.GatherResults().Select(p => (StoryGraph)p.TargetGraph);
            var filePaths = EditorAudioUtils.GetAllAudioFilePathsFromGraphs(graphs);
            FMODAudioToEventsExporter.ExportEventsToCSV(filePaths);
        }
        
        [BoxGroup(ResultSectionName, centerLabel :true), PropertyOrder(ResultSectionOrder)]
        [Button]
        public void UpdateVoiceOvers() {
            var graphs = ResultController.GatherResults().Select(p => (StoryGraph)p.TargetGraph);
            GraphConverterUtils.UpdateVoiceOvers(true, graphs.ToArray());
        }
        
        protected override bool Validate() {
            return searchActors.All(p => !p.IsEmpty);
        }

        protected override void ExecuteTool() {
            var allActors = AllElementsWithInterface<StoryNode, IStoryActorRef>()
                .Where(trio => !trio.node.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.ActorUsage) 
                               && searchActors.Any(p=>((IStoryActorRef)trio.element).ActorRef.Contains(p)))
                .Select(trio => (trio.graph, trio.node, trio.element));
            
            var graphs = allActors
                .Select(trio => trio.graph)
                .Distinct()
                .ToList();
            
            ResultController.Feed(graphs
                .Select(graph => new DefaultResultEntry(graph, null, "Graph with VO"))
                .ToList());
        }
    }
}