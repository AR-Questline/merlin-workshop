using Awaken.TG.Main.AI.Combat.Behaviours.CustomBehaviours;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Bounty: Stop Guard Interventions")]
    public class SEditorBountyStopInterventions : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SBountyStopInterventions { };
        }
    }

    public partial class SBountyStopInterventions : StoryStep {
        public override StepResult Execute(Story story) {
            GuardIntervention.StopInterventions(false);
            return StepResult.Immediate;
        }
    }
}