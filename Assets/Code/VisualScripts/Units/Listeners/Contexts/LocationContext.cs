using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Skills;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Listeners.Contexts {
    public class LocationContext : IListenerContext {
        public IModel Model => Location;
        public Location Location { get; }
        public ICharacter Character => Location?.TryGetElement<ICharacter>();
        public IAlive Alive => Location?.TryGetElement<IAlive>();
        public Skill Skill => null;
        public Item Item => null;
        public Status Status => null;

        public LocationContext(Location location) {
            Location = location;
        }
    }
    
    [UnitCategory("AR/General/Events/Context")]
    [TypeIcon(typeof(IListenerContext))]
    [UnitTitle("LocationContext")]
    [UnityEngine.Scripting.Preserve]
    public class LocationContextUnit : ARUnit {
        protected override void Definition() {
            var location = RequiredARValueInput<Location>("location");
            ValueOutput("context", flow => {
                var l = location.Value(flow);
                if (l == null) {
                    Log.Minor?.Error($"LocationContext cannot be created with null location! {flow.stack.self}");
                }
                return new LocationContext(l);
            });
        }
    }
    
    [UnitCategory("AR/General/Events/Context")]
    [TypeIcon(typeof(IListenerContext))]
    [UnitTitle("LocationContextFromSpec")]
    [UnityEngine.Scripting.Preserve]
    public class LocationContextFromSpecUnit : ARUnit {
        protected override void Definition() {
            var spec = FallbackARValueInput("spec", flow => flow.stack.self.GetComponent<LocationSpec>());
            ValueOutput("context", flow => new LocationContext(World.ByID<Location>(spec.Value(flow).GetLocationId())));
        }
    }
}