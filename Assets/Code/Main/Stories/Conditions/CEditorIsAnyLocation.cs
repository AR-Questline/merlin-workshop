using System.Linq;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Location/Location: Any Exists"), NodeSupportsOdin]
    public class CEditorIsAnyLocation : EditorCondition {
        public LocationReference locationReference;
        public bool mustBeAlive;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CIsAnyLocation {
                locationReference = locationReference,
                mustBeAlive = mustBeAlive
            };
        }
    }

    public partial class CIsAnyLocation : StoryCondition {
        public LocationReference locationReference;
        public bool mustBeAlive;
    
        public override bool Fulfilled(Story story, StoryStep step) {
            return locationReference.MatchingLocations(story).Any(Conditions);

            bool Conditions(Location location) {
                return !mustBeAlive || (location.TryGetElement<IAlive>(out var alive) && alive.IsAlive);
            }
        }
    }
}