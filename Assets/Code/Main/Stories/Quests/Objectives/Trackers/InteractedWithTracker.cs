using Awaken.TG.Main.Locations;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class InteractedWithTracker : BaseSimpleTracker<InteractedWithTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.InteractedWithTracker;

        LocationReference _interactiveLocation;

        public override void InitFromAttachment(InteractedWithTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            _interactiveLocation = spec.Location;
        }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Location.Events.Interacted, this, OnHeroInteracted);
        }
        
        void OnHeroInteracted(LocationInteractionData locationData) {
            if (_interactiveLocation.IsMatching(null, locationData.location)) {
                ChangeBy(1f);
            }
        }
    }
}