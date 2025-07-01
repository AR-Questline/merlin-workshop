using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;
using Awaken.Utility;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class RestTracker : BaseSimpleTracker<RestTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.RestTracker;

        bool _trackOnlyWhenActive;

        public override void InitFromAttachment(RestTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            _trackOnlyWhenActive = spec.TrackOnlyWhenActive;
        }

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Hero.Events.AfterHeroRested, this, OnAfterHeroRested);
        }

        void OnAfterHeroRested() {
            if (_trackOnlyWhenActive && ParentModel.State != ObjectiveState.Active) {
                return;
            }

            ChangeBy(1f);
        }
    }
}