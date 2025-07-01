using System.Linq;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.SocialServices;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Stories.Quests.Objectives.Effectors {
    public partial class AchievementEffector : Element<Objective>, IObjectiveEffector, IRefreshedByAttachment<AchievementEffectorAttachment> {
        public override ushort TypeForSerialization => SavedModels.AchievementEffector;

        [Saved] string _achievementID;
        int _microsoftId;
        int _sonyId;
        AggregationType _aggregationType;
        bool _disabled;

        public string BaseAchievementID => _achievementID;

        string AchievementIDForPlatform {
            get {
                if (PlatformUtils.IsPS5) {
                    return _sonyId.ToString();
                } else if (PlatformUtils.IsMicrosoft) {
                    return _microsoftId.ToString();
                } else {
                    return _achievementID;
                }
            }
        }

        [JsonConstructor, UnityEngine.Scripting.Preserve]
        AchievementEffector() { }
        
        public AchievementEffector(string achievementID) {
            _achievementID = achievementID;
        }
        
        public void InitFromAttachment(AchievementEffectorAttachment spec, bool isRestored) {
            _aggregationType = spec.aggregationType;
            _microsoftId = spec.microsoftId;
            _sonyId = spec.sonyId;
        }

        protected override void OnInitialize() {
            ParentModel.AfterFullyInitialized(InitTrackerListeners);
        }

        void InitTrackerListeners() {
            foreach (var tracker in ParentModel.Trackers) {
                tracker.ListenTo(Model.Events.AfterChanged, OnTrackerChanged, this);
            }
        }

        public void OnStateUpdate(QuestUtils.ObjectiveStateChange stateChange) {
            if (_disabled) return;
            var social = Services.TryGet<SocialService>();
            if (social == null) return;
            
            if (ParentModel.Trackers.Any()) {
                UpdateTrackers(social);
            }
            
            if (ParentModel.State == ObjectiveState.Completed) {
                Log.Important?.Info("Achievement completed: " + _achievementID);
                social.SetAchievement(AchievementIDForPlatform, onSuccess: TryDiscard);
                _disabled = true;
            }
        }

        void OnTrackerChanged() {
            if (_disabled) return;
            var social = Services.TryGet<SocialService>();
            if (social == null) return;

            if (ParentModel.State == ObjectiveState.Completed) {
                return;
            }
            UpdateTrackers(social);
        }

        void UpdateTrackers(SocialService social) {
            float aggregation = 0f;
                
            if (_aggregationType == AggregationType.Sum) {
                aggregation = ParentModel.Trackers.Sum(t => t.Current);
            } else if (_aggregationType == AggregationType.Max) {
                aggregation = ParentModel.Trackers.Select(t => t.Current).Max();
            }
                
            if (aggregation != 0f) {
                social.SetAchievementProgress(AchievementIDForPlatform, (int)aggregation);
            }
        }

        void TryDiscard() {
            if (!HasBeenDiscarded) {
                Discard();
            }
        }

        public enum AggregationType : byte {
            Max = 0,
            Sum = 1,
        }
    }
}