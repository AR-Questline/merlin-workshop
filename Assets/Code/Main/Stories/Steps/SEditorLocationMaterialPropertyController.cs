using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine.Scripting;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Material Property Controller"), NodeSupportsOdin]
    public class SEditorLocationMaterialPropertyController : EditorStep {
        public LocationReference locationReference;
        public bool enable;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationMaterialPropertyController {
                locationReference = locationReference,
                enable = enable
            };
        }
    }

    public partial class SLocationMaterialPropertyController : StoryStepWithLocationRequirement {
        public LocationReference locationReference;
        public bool enable;

        protected override LocationReference RequiredLocations => locationReference;

        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution(enable);
        }

        public partial class StepExecution : DeferredLocationExecution {
            public override ushort TypeForSerialization => SavedTypes.StepExecution_LocationMaterialPropertyController;

            [Saved] bool _enable;
            
            public override bool RequireVisualLoaded => true;
            
            [JsonConstructor, Preserve]
            StepExecution() { }
            
            public StepExecution(bool enable) {
                _enable = enable;
            }
            
            public override void Execute(Location location) {
                foreach (DrakeAnimatedPropertiesOverrideController controller in location.LocationView.GetComponentsInChildren<DrakeAnimatedPropertiesOverrideController>()) {
                    if (_enable) {
                        controller.StartForward();
                    } else {
                        controller.StartBackward();
                    }
                }
            }
        }
    }
}