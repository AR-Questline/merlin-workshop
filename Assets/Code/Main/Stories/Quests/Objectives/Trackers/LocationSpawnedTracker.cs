using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class LocationSpawnedTracker : BaseSimpleTracker<LocationSpawnedTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.LocationSpawnedTracker;

        LocationReference _interactiveLocation;

        public override void InitFromAttachment(LocationSpawnedTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            _interactiveLocation = spec.Location;
        }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<Location>(), this, OnModelAdded);
        }

        void OnModelAdded(Model obj) {
            if (obj is not Location location) {
                return;
            }

            if (_interactiveLocation.IsMatching(null, location)) {
                ChangeBy(1f);
            }
        }
    }
}