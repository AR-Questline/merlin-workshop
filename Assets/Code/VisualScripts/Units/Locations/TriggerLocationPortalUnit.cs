using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Locations {
    [UnitCategory("AR/Locations")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class TriggerLocationPortalUnit : ARUnit {
        protected override void Definition() {
            ARValueInput<Location> inLocation = RequiredARValueInput<Location>("location");

            DefineSimpleAction(flow => {
                Location location = inLocation.Value(flow);
                location.TryGetElement<Portal>().Execute(Hero.Current);
            });
        }
    }
}
