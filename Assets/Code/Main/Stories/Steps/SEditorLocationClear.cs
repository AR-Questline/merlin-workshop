using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Clear"), NodeSupportsOdin]
    public class SEditorLocationClear : EditorStep {
        public LocationReference locations;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationClear {
                locations = locations
            };
        }
    }

    public partial class SLocationClear : StoryStepWithLocationRequirement {
        public LocationReference locations;

        protected override LocationReference RequiredLocations => locations;

        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution();
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_LocationClear;
            
            public override void Execute(Location location) { 
                location.Clear();
            }
        }
    }
}