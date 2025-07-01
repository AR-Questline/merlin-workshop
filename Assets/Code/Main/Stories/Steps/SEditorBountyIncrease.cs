using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Templates;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Bounty: Increase"), NodeSupportsOdin]
    public class SEditorBountyIncrease : EditorStep {
        [TemplateType(typeof(CrimeOwnerTemplate))]
        public TemplateReference owner;

        [LabelWidth(130)]
        public bool unforgivableCrime;
        [HideIf(nameof(unforgivableCrime))]
        public float bountyGain = 100;
        [HideIf(nameof(unforgivableCrime))]
        public CrimeSituation situation;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SBountyIncrease {
                owner = owner,
                unforgivableCrime = unforgivableCrime,
                bountyGain = bountyGain,
                situation = situation
            };
        }
    }

    public partial class SBountyIncrease : StoryStep {
        public TemplateReference owner;
        public bool unforgivableCrime;
        public float bountyGain = 100;
        public CrimeSituation situation;
        
        public override StepResult Execute(Story story) {
            if (unforgivableCrime) {
                CrimeUtils.CommitUnforgivableCrime(owner.Get<CrimeOwnerTemplate>());
            } else if (bountyGain > 0) {
                var crimeSource = new FakeCrimeSource(owner.Get<CrimeOwnerTemplate>(), bountyGain);
                Crime.Custom(crimeSource, situation).TryCommitCrime(); // TODO: check all useages of this step
            }
            return StepResult.Immediate;
        }
    }
}