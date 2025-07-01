using System;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class CyclicAction : Element<Location>, IRefreshedByAttachment<CyclicActionAttachment> {
        public override ushort TypeForSerialization => SavedModels.CyclicAction;

        [Saved] ARDateTime _nextActionTime;
        CyclicActionAttachment _spec;
        TimedEvent _nextIntervalEvent;
        
        bool ShouldPerformAction => World.Any<GameRealTime>().WeatherTime > _nextActionTime;

        public void InitFromAttachment(CyclicActionAttachment spec, bool isRestored) {
            _spec = spec;
            if (_spec.ActionInterval.TotalMinutes == 0) {
                Log.Minor?.Error($"Cyclic Action {_spec.gameObject} cycle is set to 0!", _spec);
                return;
            }
            
            if (!isRestored) {
                _nextActionTime = GetFirstActionTime();
            }
            
            //If hero or game skipped time, he is not "witnessing" the events.
            World.EventSystem.ListenTo(EventSelector.AnySource, GameRealTime.Events.BeforeTimeSkipped, this, OnTimeSkipped);

            //If should perform right now, it means he hasn't witnessed the events.
            if (ShouldPerformAction) {
                //Needs to be delayed because all other locations must be initialized
                DelayPerformNecessaryActions().Forget();
                return;
            }
            
            _nextIntervalEvent = new TimedEvent(_nextActionTime.Date, PerformAllActions);
            World.Any<GameTimeEvents>().AddEvent(_nextIntervalEvent);
        }

        void OnTimeSkipped(GameRealTime.TimeSkipData data) {
            var timeAfterRest = World.Any<GameRealTime>().WeatherTime.Date.AddMinutes(data.timeSkippedInMinutes);
            if (timeAfterRest > _nextActionTime) {
                World.Any<GameTimeEvents>()?.RemoveEvent(_nextIntervalEvent);
                _nextIntervalEvent = null;
                PerformNecessaryActions();
            }
        }

        async UniTaskVoid DelayPerformNecessaryActions() {
            if (!await AsyncUtil.DelayFrame(this, 1)) {
                return;
            }
            PerformNecessaryActions();
        }

        void PerformAllActions() {
            if (_spec.CheckIfWitnessingTheCycle) {
                foreach (var location in _spec.LocationsActivatedWhenWitnessing.ToArray()) {
                    HeroInteraction.StartInteraction(null, location, out _);
                }
            }
            PerformNecessaryActions();
        }
        
        void PerformNecessaryActions() {
            if (_spec.CanNecessaryActionsBePerformedMultipleTimesOnRestore) {
                PerformNecessaryActionsMultipleTimes();
            } else {
                PerformNecessaryActionsOneTime();
            }
            _nextIntervalEvent = new TimedEvent(_nextActionTime.Date, PerformAllActions);
            World.Any<GameTimeEvents>().AddEvent(_nextIntervalEvent);
        } 
        
        void PerformNecessaryActionsMultipleTimes() {
            var time = World.Any<GameRealTime>().WeatherTime;
            while (_nextActionTime <= time) {
                foreach (var location in _spec.LocationsAlwaysActivated.ToArray()) {
                    if (location is not { HasBeenDiscarded: false }) {
                        continue;
                    }
                    HeroInteraction.StartInteraction(null, location, out _);
                }
                _nextActionTime = GetNextActionTime(_nextActionTime);
            }
        }

        void PerformNecessaryActionsOneTime() {
            foreach (var location in _spec.LocationsAlwaysActivated.ToArray()) {
                if (location is not { HasBeenDiscarded: false }) {
                    continue;
                }
                HeroInteraction.StartInteraction(null, location, out _);
            }
            var time = World.Any<GameRealTime>().WeatherTime;
            while (_nextActionTime <= time) {
                _nextActionTime = GetNextActionTime(_nextActionTime);
            }
        }
        
        ARDateTime GetFirstActionTime() {
            var currentTime = World.Any<GameRealTime>().WeatherTime;
            var tempFirstActionTime = new ARDateTime(new DateTime(currentTime.Year, currentTime.Month, currentTime.DayOfTheMonth, _spec.Hour, _spec.Minutes, 0));
            var firstActionTime = tempFirstActionTime;
            
            if (firstActionTime > currentTime) {
                while (tempFirstActionTime > currentTime) {
                    firstActionTime = tempFirstActionTime;
                    tempFirstActionTime = GetPreviousActionTime(tempFirstActionTime);
                } 
            } else {
                while (currentTime > tempFirstActionTime) {
                    tempFirstActionTime = GetNextActionTime(tempFirstActionTime);
                    firstActionTime = tempFirstActionTime;
                } 
            }
            
            return firstActionTime;
        }

        ARDateTime GetNextActionTime(ARDateTime time) {
            return time + _spec.ActionInterval;
        }
        
        ARDateTime GetPreviousActionTime(ARDateTime time) {
            return time - _spec.ActionInterval;
        }
        
        protected override void OnDiscard(bool fromDomainDrop) {
            World.Any<GameTimeEvents>()?.RemoveEvent(_nextIntervalEvent);
            _nextIntervalEvent = null;
        }
    }
}
