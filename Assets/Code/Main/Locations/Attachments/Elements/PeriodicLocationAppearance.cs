using Awaken.Utility;
using System;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class PeriodicLocationAppearance : Element<Location>, IRefreshedByAttachment<PeriodicLocationAppearanceAttachment> {
        public override ushort TypeForSerialization => SavedModels.PeriodicLocationAppearance;

        const int AppearanceWindowMinutes = 30;
        
        PeriodicLocationAppearanceAttachment _spec;
        GameTimeEvents _timeEvents;
        
        TimedEvent[] _appearanceEvents = Array.Empty<TimedEvent>();
        bool _isAppearingNow;
        
        public void InitFromAttachment(PeriodicLocationAppearanceAttachment spec, bool isRestored) {
            _spec = spec;
        }

        protected override void OnInitialize() {
            base.OnInitialize();
            
            _timeEvents = World.Only<GameTimeEvents>();
            RegisterAppearanceEvents();
        }

        void RegisterAppearanceEvents() {
            _appearanceEvents = new TimedEvent[_spec.Appearances.Length];
            for (int i = 0; i < _spec.Appearances.Length; i++) {
                RegisterNewAppearanceEvent(i);
            }
        }

        void RegisterNewAppearanceEvent(int appearanceIndex) {
            var timeEvent = new TimedEvent(GetNextAppearanceTime(appearanceIndex), () => OnAppearance(appearanceIndex));
            
            if (_appearanceEvents[appearanceIndex] != null) {
                _timeEvents.RemoveEvent(_appearanceEvents[appearanceIndex]);
            }
            _appearanceEvents[appearanceIndex] = timeEvent;
            _timeEvents.AddEvent(_appearanceEvents[appearanceIndex]);
        }
        
        DateTime GetNextAppearanceTime(int appearanceIndex) {
            var appearance = _spec.Appearances[appearanceIndex];
            
            var currentDayTime = World.Only<GameRealTime>().WeatherTime;
            var currentDayTimeDaysCount = TimeSpan.FromSeconds(currentDayTime.TotalSeconds).Days;
            var randomAppearanceTimeInDays = appearance.time.GetTime().TotalDays;
            var appearanceTime = new DateTime(TimeSpan.FromDays(currentDayTimeDaysCount + randomAppearanceTimeInDays).Ticks);
            
            var lastAppearance = _appearanceEvents[appearanceIndex];
            if (lastAppearance != null && lastAppearance.Time >= appearanceTime) {
                appearanceTime = appearanceTime.AddDays(1);
            }
            
            var randomDelayInDays = new TimeSpan(RandomUtil.UniformInt(0, (int)appearance.randomDelay.Ticks));
            return appearanceTime + randomDelayInDays;
        }
        
        void OnAppearance(int appearanceIndex) {
            if (ShouldAppear(appearanceIndex)) {
                AppearLocation(appearanceIndex).Forget();
            }
            
            RegisterNewAppearanceEvent(appearanceIndex);
        }

        bool ShouldAppear(int appearanceIndex) {
            if (_isAppearingNow) {
                return false;
            }

            var appearance = _spec.Appearances[appearanceIndex];
            
            var delayedTimeSpan = World.Only<GameRealTime>().WeatherTime - _appearanceEvents[appearanceIndex].Time;
            if (delayedTimeSpan.Minutes >= AppearanceWindowMinutes) {
                return false;
            }
            
            float distanceToHero = Vector3.Distance(GetAppearancePoint(appearanceIndex).position, Hero.Current.Coords);
            if (distanceToHero < appearance.minDistanceToHero || distanceToHero > appearance.maxDistanceToHero) {
                return false;
            }

            return RandomUtil.WithProbability(appearance.chancesToAppear);
        }

        async UniTaskVoid AppearLocation(int appearanceIndex) {
            var appearance = _spec.Appearances[appearanceIndex];
            var locationTemplate = appearance.LocationToAppear;
            if (locationTemplate == null) {
                return;
            }
            
            _isAppearingNow = true;

            var spawnPoint = GetAppearancePoint(appearanceIndex);
            var location = locationTemplate.SpawnLocation(spawnPoint.position, spawnPoint.rotation);
            location.MarkedNotSaved = true;

            if (!await AsyncUtil.DelayTime(this, appearance.duration)) {
                location.Discard();
                return;
            }
            
            _isAppearingNow = false;
            location.Discard();
        }
        
        Transform GetAppearancePoint(int appearanceIndex) {
            var appearancePoint = _spec.Appearances[appearanceIndex].appearPoint;
            return appearancePoint != null ? appearancePoint : ParentModel.ViewParent;
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            for (int i = 0; i < _appearanceEvents.Length; i++) {
                _timeEvents.RemoveEvent(_appearanceEvents[i]);
                _appearanceEvents[i] = null;
            }
            _appearanceEvents = Array.Empty<TimedEvent>();
            _timeEvents = null;
            
            base.OnDiscard(fromDomainDrop);
        }
    }
}