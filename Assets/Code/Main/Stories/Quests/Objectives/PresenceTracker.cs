using Awaken.Utility;
using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Awaken.TG.Main.Stories.Quests.Objectives {
    public partial class PresenceTracker : Model {
        public override ushort TypeForSerialization => SavedModels.PresenceTracker;

        public override Domain DefaultDomain => Domain.Gameplay;
        static DateTime CurrentTime => (DateTime)World.Only<GameRealTime>().WeatherTime;
        
        PresenceTrackerData _data;

        [Saved] public ActorRef Owner { get; private set; }
        [Saved] int _currentSceneIndex;
        [Saved] int _currentAttachmentGroupIndex;
        
        TimedEvent _nextIntervalEvent;

        public event Action PresenceUpdated;

        public SceneReference CurrentScene => _data.presenceTargetScenes[_currentSceneIndex].GetTargetScene(CurrentTime, _currentAttachmentGroupIndex);

        [JsonConstructor, Preserve]
        public PresenceTracker() { }

        public PresenceTracker(in PresenceTrackerData data, PresenceTrackerService service) {
            Owner = data.actor;
            InitBase(data, service);
        }

        public void Initialize(in PresenceTrackerData data, PresenceTrackerService service) {
            if (data.actor != Owner) {
                throw new Exception("Trying to initialize PresenceTracker with wrong data");
            }
            InitBase(data, service);
        }
        
        void InitBase(in PresenceTrackerData data, PresenceTrackerService service) {
            this._data = data;
            service.PresenceUpdated += OnPresenceUpdated;
            OnIntervalChanged();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            if (fromDomainDrop) return;
            World.Services.Get<PresenceTrackerService>().PresenceUpdated -= OnPresenceUpdated;
        }

        void OnPresenceUpdated(in PresenceTrackerService.PresenceUpdate presenceUpdated) {
            if (presenceUpdated.groupName != null) {
                TryAttachmentGroupUpdate(presenceUpdated);
                return;
            }
            
            // RichLabelUpdate
            for (int index = 0; index < _data.presenceTargetScenes.Count; index++) {
                PresenceTargetScene dataPresenceTargetScene = _data.presenceTargetScenes[index];
                if (dataPresenceTargetScene.RichLabelMatches(presenceUpdated)) {
                    _currentSceneIndex = index;
                    _currentAttachmentGroupIndex = 0;
                    PresenceUpdated?.Invoke();
                    OnIntervalChanged();
                    break;
                }
            }
        }

        void TryAttachmentGroupUpdate(PresenceTrackerService.PresenceUpdate presenceupdated) {
            if (presenceupdated.actor != Owner) {
                return;
            }
            var groupIndex = _data.presenceTargetScenes[_currentSceneIndex].AttachmentGroupReliantIndex(presenceupdated.groupName);
            
            if (!presenceupdated.enable) {
                // we disabled a group that was not active, ignore
                if (groupIndex != _currentAttachmentGroupIndex) {
                    return;
                }
                // we disabled the group that was active, we fall back to the default group. We do not have a history stack
                groupIndex = 0;
            }
            _currentAttachmentGroupIndex = groupIndex;
                
            PresenceUpdated?.Invoke();
            OnIntervalChanged();
        }

        void OnIntervalChanged() {
            GameTimeEvents gameTimeEvents = World.Any<GameTimeEvents>();
            if (_nextIntervalEvent != null) {
                gameTimeEvents.RemoveEvent(_nextIntervalEvent);
                _nextIntervalEvent = null;
            }
            PresenceTargetScene dataPresenceTargetScene = _data.presenceTargetScenes[_currentSceneIndex];
            if (dataPresenceTargetScene.HasNoSceneChangeIntervals) {
                return;
            }
            if (!dataPresenceTargetScene.TryGetNextIntervalStartTime(CurrentTime, _currentAttachmentGroupIndex, out var nextStart)) {
                return;
            }
            _nextIntervalEvent = new TimedEvent(nextStart, OnIntervalChanged);
            
            gameTimeEvents.AddEvent(_nextIntervalEvent);
            PresenceUpdated?.Invoke();
        }
    }
}