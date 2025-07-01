using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Trackers {
    public partial class StatTracker : BaseSimpleTracker<StatTrackerAttachment> {
        public override ushort TypeForSerialization => SavedModels.StatTracker;

        [Saved] public StatType TrackedStat { get; private set; }
        StatTrackType _trackType;
        Stat _stat;

        public override void InitFromAttachment(StatTrackerAttachment spec, bool isRestored) {
            base.InitFromAttachment(spec, isRestored);
            TrackedStat = spec.StatRef;
            _trackType = spec.TrackType;
        }

        protected override void OnInitialize() {
            _stat = RetrieveStat();
            if (_stat == null) {
                Log.Important?.Error($"Can't find stat owner in quest {ParentModel.ParentModel.DisplayName}, " +
                               $"objective {ParentModel.Name} (guid: {ParentModel.Guid}), for stat {TrackedStat.EnumName}");
                return;
            }
            
            _stat.Owner.ListenTo(Stat.Events.StatChangedBy(TrackedStat), StatChanged, this);
            StatChanged(new Stat.StatChange(_stat, 0f));
        }

        Stat RetrieveStat() {
            return Hero.Current.Stat(TrackedStat);
        }

        void StatChanged(Stat.StatChange change) {
            if (_trackType == StatTrackType.Current) {
                SetTo(_stat.ModifiedValue);
            } else if (_trackType == StatTrackType.Gain) {
                if (change.value > 0) {
                    ChangeBy(change.value);
                }
            } else if (_trackType == StatTrackType.Loss) {
                if (change.value < 0) {
                    ChangeBy(-change.value);
                }
            }
        }
    }
}
