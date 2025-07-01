using System.Collections.Generic;
using Awaken.TG.Main.Locations.Pickables;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Utility.Availability {
    public static class AvailabilityInitialization {
        static readonly List<AvailabilityBase> ToRefreshAvailability = new();

        public static void RefreshAvailability(AvailabilityBase availability) {
            if (World.Services.TryGet(out PickableService _)) {
                // We are waiting for Pickable Service cause it's one of the last Services to initialize, it may be later reworked for some better solution.
                return;
            } else {
                ToRefreshAvailability.Add(availability);
            }
        }

        public static void RemoveFromRefreshAwaiting(AvailabilityBase availability) {
            ToRefreshAvailability.Remove(availability);
        }

        public static void InitializeWaiting() {
            foreach (var availability in ToRefreshAvailability) {
                availability.Refresh();
            }
            ToRefreshAvailability.Clear();
        }
    }
}