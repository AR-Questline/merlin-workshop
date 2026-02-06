using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Main.Locations;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Locations {
    [UnitCategory("AR/Locations")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class LocationMaterialPropertyControllerUnit : ARUnit {
        protected override void Definition() {
            var locationReference = RequiredARValueInput<LocationReference>("location");
            var enableInput = RequiredARValueInput<bool>("enable");
            
            DefineSimpleAction(flow => {
                var location = locationReference.Value(flow).MatchingLocations(null);
                var enable = enableInput.Value(flow);
                foreach (var loc in location) {
                    foreach (DrakeAnimatedPropertiesOverrideController controller in loc.LocationView.GetComponentsInChildren<DrakeAnimatedPropertiesOverrideController>()) {
                        if (enable) {
                            controller.StartForward();
                        } else {
                            controller.StartBackward();
                        }
                    }
                }
            });
        }
    }
}