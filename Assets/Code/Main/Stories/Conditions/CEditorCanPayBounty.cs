using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;

namespace Awaken.TG.Main.Stories.Conditions {
    /// <summary>
    /// Check if hero has enough wealth to pay bounty
    /// </summary>
    [Element("Hero: Can Pay Bounty")]
    public class CEditorCanPayBounty : EditorCondition {
        public LocationReference guard;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CCanPayBounty {
                guard = guard
            };
        }
    }

    public partial class CCanPayBounty : StoryCondition {
        public LocationReference guard;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            if (story == null) {
                return false;
            }
            if (StoryUtils.TryGetCrimeOwnerTemplate(story, guard, out var crimeOwner)) {
                return CrimeUtils.Bounty(crimeOwner) <= story.Hero.Wealth.ModifiedValue;
            }
            return false;
        }
    }
}