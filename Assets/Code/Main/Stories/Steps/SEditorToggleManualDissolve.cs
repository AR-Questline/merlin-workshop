using Awaken.TG.Assets;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Deferred;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.Main.Utility.VFX;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Toggle Manual Dissolve"), NodeSupportsOdin]
    public class SEditorToggleManualDissolve : EditorStep {
        public LocationReference locationReference;
        public bool state;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SToggleManualDissolve {
                locationReference = locationReference,
                state = state,
            };
        }
    }

    public partial class SToggleManualDissolve : StoryStepWithLocationRequirement {
        public LocationReference locationReference;
        public bool state;
        
        protected override LocationReference RequiredLocations => locationReference;
        
        protected override DeferredLocationExecution GetStepExecution(Story story) {
            return new StepExecution() {
                state = state,
            };
        }

        public partial class StepExecution : DeferredLocationExecution {
            public bool state;
            
            public override ushort TypeForSerialization => SavedTypes.StepExecution_ToggleManualDissolve;

            public override void Execute(Location location) {
                var dissolve = location.MainView.transform.GetComponentInChildren<VCManualDissolveController>();
                if (dissolve) {
                    dissolve.SwitchVisibility(state);
                } else {
                    Log.Important?.Error($"Location {LogUtils.GetDebugName(location)} does not have a VCManualDissolveController.");
                }
            }
        }
    }
}