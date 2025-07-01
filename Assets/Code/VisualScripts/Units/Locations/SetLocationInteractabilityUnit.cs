using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Utility.RichEnums;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Locations {
    [UnitCategory("AR/Locations")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SetLocationInteractabilityUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable, RichEnumExtends(typeof(LocationInteractability))]
        public RichEnumReference interactability;
        
        protected override void Definition() {
            ARValueInput<Location> inLocation = RequiredARValueInput<Location>("location");

            DefineSimpleAction(flow => {
                Location location = inLocation.Value(flow);
                location.SetInteractability(interactability.EnumAs<LocationInteractability>());
            });
        }
    }
}
