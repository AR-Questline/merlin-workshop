using Awaken.TG.Main.Locations.Discovery;
using Awaken.TG.Main.Utility.VFX;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Locations.CustomBehaviours {
    public class FastTravelActivator : ViewComponent<Location> {
        protected override void OnAttach() {
            if (Target.TryGetElement(out LocationDiscovery locationDiscovery)) {
                if (!locationDiscovery.Discovered) {
                    Target.AfterFullyInitialized(() => {
                        locationDiscovery.ListenTo(LocationDiscovery.Events.LocationDiscovered, OnFastTravelDiscovered, this);
                    }, Target);
                }
            } else {
                Log.Minor?.Error($"Location {Target.DisplayName} does not have a LocationDiscovery element", this);
            }
        }

        static void OnFastTravelDiscovered(Location location) {
            location.TryGetElement<ActivateFastTravelElement>()?.ActivateTween();
        }
    }
}