using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Actor: Change State")]
    public class SEditorActorState : EditorStep, IStoryActorRef {
        [LabelText("Actor")]
        public ActorRef actorRef;

        [ActorRef(nameof(actorRef))]
        public ActorStateRef stateName;
        
        public ActorRef[] ActorRef => new[] { actorRef };

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SActorState {
                actorRef = actorRef,
                stateName = stateName
            };
        }
    }

    public partial class SActorState : StoryStep {
        public ActorRef actorRef;
        public ActorStateRef stateName;
        
        public override StepResult Execute(Story story) {
            World.Services.Get<ActorsRegister>().SetState(actorRef, stateName);
            return StepResult.Immediate;
        }
    }
}