using System;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all actors that are set to NONE in SText")]
    public class STextActorChecker : StoryGraphUtilityTool<DefaultResult<ActorCheckerResultEntry>, ActorCheckerResultEntry> {
        protected override bool Validate() {
            return true;
        }
        
        protected override void ExecuteTool() {
            AllElementsOrderByGraphName<StoryNode, SEditorText>().ForEach(trio => CheckActor(trio.node, trio.element));
        }
        
        void CheckActor(StoryNode storyNode, SEditorText sEditorText) {
            if (storyNode.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.STextActorChecker)) return;
            if (((ITemplate) storyNode.Graph).TemplateType is TemplateType.ForRemoval) return;
            
            if (!sEditorText.hasVoice) return;
            if (storyNode.Graph.name.Contains("Bark")) return;
            
            if (sEditorText.actorRef.IsNone()) {
                ResultController.Feed(new ActorCheckerResultEntry(sEditorText.Parent.Graph, storyNode, true));
            }
            
            if (sEditorText.targetActorRef.IsNone()) {
                ResultController.Feed(new ActorCheckerResultEntry(sEditorText.Parent.Graph, storyNode, false));
            }
        }
    }

    [Serializable]
    public class ActorCheckerResultEntry : DefaultResultEntry {
        [SerializeField] [ReadOnly] ActorType actorType;

        public ActorCheckerResultEntry(NodeGraph graph, StoryNode node, bool talker) : base(graph: graph, node: node) {
            actorType = talker ? ActorType.Talker : ActorType.Target;
        }

        enum ActorType {
            Talker,
            Target
        }
    }
}
