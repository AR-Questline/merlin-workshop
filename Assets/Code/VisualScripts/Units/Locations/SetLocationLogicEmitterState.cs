using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Locations {
    [UnitCategory("AR/Locations")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SetLocationLogicEmitterState : ARUnit {
        protected override void Definition() {
            ARValueInput<Location> inLocation = RequiredARValueInput<Location>("location");
            ARValueInput<bool> stateValue = OptionalARValueInput<bool>("state");

            DefineSimpleAction(flow => {
                Location location = inLocation.Value(flow);
                location.TryGetElement<LogicEmitterAction>()?.ChangeState(stateValue.HasValue ? stateValue.Value(flow) : null);
            });
        }
    }
}
