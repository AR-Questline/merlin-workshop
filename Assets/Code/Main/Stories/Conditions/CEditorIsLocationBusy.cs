using System.Linq;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Conditions {
    [Element("Location/Location: Is busy")]
    public class CEditorIsLocationBusy : EditorCondition {
        public LocationReference locationReference;

        protected override StoryCondition CreateRuntimeConditionImpl(StoryGraphParser parser) {
            return new CIsLocationBusy {
                locationReference = locationReference
            };
        }
    }

    public partial class CIsLocationBusy : StoryCondition {
        public LocationReference locationReference;
        
        public override bool Fulfilled(Story story, StoryStep step) {
            return locationReference.MatchingLocations(story).All(l => l.IsBusy);
        }
    }
}
