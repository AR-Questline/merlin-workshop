using System;
using System.Linq;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all usage of desired Actor\n" +
                 "1. Provide Actor reference\n" +
                 "2. Click Execute button\n" +
                 "3. You can check usage type in the results list")]
    public class ActorUsageFinder : StoryGraphUtilityTool<SearchResult<ActorResultEntry>, ActorResultEntry> {
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField]
        ActorRef searchedActor;
        
        protected override bool Validate() {
            return !searchedActor.IsEmpty;
        }
        
        protected override void ExecuteTool() {
            ResultController.SetCurrentlySearched(ActorsRegister.Get.Editor_GetActorName(searchedActor.guid));
            
            var allActors = AllElementsWithInterface<StoryNode, IStoryActorRef>()
                    .Where(trio => !trio.node.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.ActorUsage) 
                            && ((IStoryActorRef)trio.element).ActorRef.Contains(searchedActor))
                    .Select(trio => (trio.graph, trio.node, trio.element));
            
            foreach (var valueTuple in allActors.OrderBy(tuple => tuple.graph.name)) {
                if (valueTuple.Item3 is SEditorText sText) {
                    string note = sText.actorRef == searchedActor ? "Actor is a talker" : "Actor is a listener";
                    ResultController.Feed(new ActorResultEntry(valueTuple.graph, valueTuple.node, sText.GetType().Name, note));
                    continue;
                }
                
                ResultController.Feed(new ActorResultEntry(valueTuple.graph, valueTuple.node, valueTuple.Item3.GetType().Name));
            } 
        }
    }
    
    [Serializable]
    public class ActorResultEntry : DefaultResultEntry {
        [SerializeField, ReadOnly] string usageName;

        public ActorResultEntry(NodeGraph graph, StoryNode node, string stepName, string notes = "") : base(graph, node, notes){
            usageName = stepName;
        }
    }
}
