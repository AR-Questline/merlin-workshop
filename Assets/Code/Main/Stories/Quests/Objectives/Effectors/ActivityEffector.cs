using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.MVC.Elements;
using Awaken.Utility;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Effectors {
    public partial class ActivityEffector : Element<Objective>, IObjectiveEffector, IRefreshedByAttachment<ActivityEffectorAttachment> {
        public override ushort TypeForSerialization => SavedModels.ActivityEffector;

        string _activityId;
        bool _disabled;

        public string ActivityId => _activityId;
        
        public void InitFromAttachment(ActivityEffectorAttachment spec, bool isRestored) {
            _activityId = spec.activityId;
        }

        public void OnStateUpdate(QuestUtils.ObjectiveStateChange stateChange) {
#if UNITY_PS5
            if (_disabled) return;
            var social = Services.TryGet<SocialService>();
            if (social is not SocialServices.PlayStationServices.PlayStationSocialService psService) return;

            if (ParentModel.State == ObjectiveState.Completed) {
                Log.Marking?.Warning("Activity completed: " + _activityId);
                psService.EndActivity(_activityId, onSuccess: TryDiscard);
                _disabled = true;
            }
#endif
        }

        void TryDiscard() {
            if (!HasBeenDiscarded) {
                Discard();
            }
        }
    }
}