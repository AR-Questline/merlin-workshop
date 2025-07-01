using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Mount: Change Hero Owner"), NodeSupportsOdin]
    public class SEditorChangeHeroMountOwner : EditorStep {
        public LocationReference locationRef = new() {targetTypes = TargetType.Self};
        public bool isHeroMount = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SChangeHeroMountOwner {
                locationRef = locationRef,
                isHeroMount = isHeroMount
            };
        }
    }

    public partial class SChangeHeroMountOwner : StoryStep {
        public LocationReference locationRef = new() {targetTypes = TargetType.Self};
        public bool isHeroMount = true;
        
        public override StepResult Execute(Story story) {
            foreach (var location in locationRef.MatchingLocations(story)) {
                location.TryGetElement<MountElement>()?.MarkAsHeroMount(isHeroMount);
            }
            return StepResult.Immediate;
        }
    }
}