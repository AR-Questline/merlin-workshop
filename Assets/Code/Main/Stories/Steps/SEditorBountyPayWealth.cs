using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility.Collections;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Bounty: Guard Pay"), NodeSupportsOdin]
    public class SEditorBountyPayWealth : EditorStep {
        public LocationReference guard;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SBountyPayWealth {
                guard = guard
            };
        }
    }

    public partial class SBountyPayWealth : StoryStep {
        public LocationReference guard;
        
        public override StepResult Execute(Story story) {
            if (StoryUtils.TryGetCrimeOwnerTemplate(story, guard, out var crimeOwner)) {
                CrimePenalties.PayBounty(crimeOwner);
            }
            return StepResult.Immediate;
        }

        public override void AppendKnownEffects(Story story, ref StructList<string> effects) {
            if (StoryUtils.TryGetCrimeOwnerTemplate(story, guard, out var crimeOwner)) {
                effects.Add($"{CurrencyStatType.Wealth.DisplayName}: {CrimeUtils.Bounty(crimeOwner)}");
            }
        }

        public override StepRequirement GetRequirement() {
            return api => {
                if (StoryUtils.TryGetCrimeOwnerTemplate(api, guard, out var crimeOwner)) {
                    return Hero.Current.Wealth.ModifiedInt >= CrimeUtils.Bounty(crimeOwner);
                }
                return true;
            };
        }
    }
}