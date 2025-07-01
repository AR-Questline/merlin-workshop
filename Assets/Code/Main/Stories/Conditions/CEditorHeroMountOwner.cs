using System.Linq;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Game/Mount: Check Hero Owner"), NodeSupportsOdin]
    public class CEditorHeroMountOwner : EditorCondition {
        public LocationReference locationRef = new() { targetTypes = TargetType.Self };

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CHeroMountOwner {
                locationRef = locationRef,
            };
        }
    }
    
    public partial class CHeroMountOwner : StoryCondition {
        public LocationReference locationRef;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return locationRef.MatchingLocations(story).Any(IsLocationHeroOwnedMount);
        }

        bool IsLocationHeroOwnedMount(Location location) {
            return location.TryGetElement(out MountElement mount) && mount.IsHeroMount;
        }
    }
}