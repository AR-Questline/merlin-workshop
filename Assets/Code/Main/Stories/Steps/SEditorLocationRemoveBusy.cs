using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.Utility;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Remove busy"), NodeSupportsOdin]
    public class SEditorLocationRemoveBusy : EditorStep {
        public LocationReference locationReference;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationRemoveBusy {
                locationReference = locationReference
            };
        }
    }

    public partial class SLocationRemoveBusy : StoryStepWithLocationRequirement {
        public LocationReference locationReference;

        protected override LocationReference RequiredLocations => locationReference;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution();
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_LocationRemoveBusy;
            
            public override void Execute(Location location) {
                location.RemoveElementsOfType<Busy>();
            }
        }
    }
}
