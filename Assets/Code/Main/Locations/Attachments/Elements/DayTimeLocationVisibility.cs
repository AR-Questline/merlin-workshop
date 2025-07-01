using System;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.Timing;
using Awaken.TG.Main.Utility.Tags;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Times;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class DayTimeLocationVisibility : Element<Location>, IRefreshedByAttachment<DayTimeLocationVisibilityAttachment> {
        public override ushort TypeForSerialization => SavedModels.DayTimeLocationVisibility;

        FlagLogic _visibleFlag;
        ARTimeOfDayIntervalRuntime[] _intervals;
        
        TimedEvent _nextTimeEvent;
        
        public void InitFromAttachment(DayTimeLocationVisibilityAttachment spec, bool isRestored) {
            _visibleFlag = spec.VisibleFlag;
            _intervals = ArrayUtils.Select(spec.VisibleTimes, t => new ARTimeOfDayIntervalRuntime(t));
            Array.Sort(_intervals, (lhs, rhs) => lhs.From.CompareTo(rhs.From));
        }

        protected override void OnInitialize() {
            if (_visibleFlag.HasFlag) {
                World.EventSystem.ListenTo(EventSelector.AnySource, StoryFlags.Events.UniqueFlagChanged(_visibleFlag.Flag), this, _ => RefreshInteractability());
            }
            OnRefreshTime();
        }

        void OnRefreshTime() {
            RefreshInteractability();
            
            var nextTime = GetNextRefreshTime((DateTime)World.Only<GameRealTime>().WeatherTime);
            _nextTimeEvent = new TimedEvent(nextTime, OnRefreshTime);
            World.Any<GameTimeEvents>()?.AddEvent(_nextTimeEvent);
        }

        void RefreshInteractability() {
            var time = World.Only<GameRealTime>().WeatherTime.Date.TimeOfDay;
            ParentModel.SetInteractability(GetInteractability(time));
        }
        
        LocationInteractability GetInteractability(TimeSpan time) {
            if (_visibleFlag.Get(true) == false) {
                return LocationInteractability.Hidden;
            }
            foreach (var interval in _intervals) {
                if (interval.Contains(time)) {
                    return LocationInteractability.Active;
                }
            }
            return LocationInteractability.Hidden;
        }
        
        DateTime GetNextRefreshTime(DateTime currentTime) {
            var beginningOfDay = currentTime.Date;
            foreach (var interval in _intervals) {
                var from = beginningOfDay + interval.From;
                if (from > currentTime) {
                    return from;
                }
                var to = beginningOfDay + interval.To;
                if (to > currentTime) {
                    return to;
                }
            }
            return beginningOfDay.AddDays(1) + _intervals[^1].To;
        } 
        
        protected override void OnDiscard(bool fromDomainDrop) {
            var gameTimeEvents = World.Any<GameTimeEvents>();
            if (gameTimeEvents != null) {
                gameTimeEvents.RemoveEvent(_nextTimeEvent);
            }
            _nextTimeEvent = null;
        }
    }
}