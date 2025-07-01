using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Actions;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Locations {
    [UnitCategory("AR/Locations")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class LocationActionFinishUnit : ARUnit {
        protected override void Definition() {
            ARValueInput<Location> inLocation = RequiredARValueInput<Location>("location");

            DefineSimpleAction(flow => {
                Location location = inLocation.Value(flow);
                location.TryGetElement<AbstractLocationAction>()?.FinishInteraction(Hero.Current, location);
            });
        }
    }
}
