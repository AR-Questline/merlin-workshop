using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Bounty: Go To Jail"), NodeSupportsOdin]
    public class SEditorBountyGoToJail : EditorStep {
        public LocationReference guard;
        public bool payNormalBounty;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SBountyGoToJail {
                guard = guard,
                payNormalBounty = payNormalBounty
            };
        }
    }

    public partial class SBountyGoToJail : StoryStep {
        public LocationReference guard;
        public bool payNormalBounty;
        
        public override StepResult Execute(Story story) {
            if (StoryUtils.TryGetCrimeOwnerTemplate(story, guard, out var crimeOwner)) {
                if (payNormalBounty) {
                    CrimePenalties.GoToPrisonPeacefully(crimeOwner, 1f);
                } else {
                    CrimePenalties.GoToPrisonPeacefully(crimeOwner);
                }
            }
            return StepResult.Immediate;
        }
    }
}