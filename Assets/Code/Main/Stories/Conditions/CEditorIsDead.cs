using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Actor: Is Dead check")]
    public class CEditorIsDead : EditorCondition {
        public ActorRef actorRef;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CIsDead {
                actorRef = actorRef
            };
        }
    }
    
    public partial class CIsDead : StoryCondition {
        public ActorRef actorRef;

        public override bool Fulfilled(Story story, StoryStep step) {
            return !World.Services.Get<NpcRegistry>().IsAlive(actorRef);
        }
    }
}