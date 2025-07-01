using System;
using System.Linq;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Attachments.Attachment;
using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.Times;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Locations.Attachments.Elements {
    public partial class CyclicStateAction : Element<Location>, IRefreshedByAttachment<CyclicStateActionAttachment> {
        public override ushort TypeForSerialization => SavedModels.CyclicStateAction;

        CyclicStateDatum[] _cyclicStates;
        ARDateTime _nextActionTime;
        CyclicStateActionAttachment _spec;
        TimedEvent _nextIntervalEvent;
        int _currentStateIndex;

        public void InitFromAttachment(CyclicStateActionAttachment spec, bool isRestored) {
            _spec = spec;
            _cyclicStates = spec.CyclicStates;
        }

        protected override void OnInitialize() {
            if (!_cyclicStates.Any()) {
                Log.Minor?.Error($"Cyclic State Action {_spec.gameObject} state list is empty!", _spec);
                return;
            }
            
            //If hero or game skipped time, he is not "witnessing" the events.
            World.EventSystem.ListenTo(EventSelector.AnySource, GameRealTime.Events.BeforeTimeSkipped, this, OnTimeSkipped);

            //If should always update state when initialized.
            DelayPerformActions().Forget();
        } 

        void OnTimeSkipped(GameRealTime.TimeSkipData data) {
            var timeAfterRest = World.Any<GameRealTime>().WeatherTime.Date.AddMinutes(data.timeSkippedInMinutes);
            if (timeAfterRest > _nextActionTime) {
                World.Any<GameTimeEvents>()?.RemoveEvent(_nextIntervalEvent);
                _nextIntervalEvent = null;
                PerformActions(timeAfterRest);
            }
        }

        async UniTaskVoid DelayPerformActions() {
            if (!await AsyncUtil.DelayFrame(this, 1)) {
                return;
            }
            PerformActions();
        }

        void PerformActions() {
            PerformActions(World.Any<GameRealTime>().WeatherTime);
        }
        
        void PerformActions(ARDateTime currentTime) {
            _currentStateIndex = GetCurrentCyclicStateIndex(currentTime);
            foreach (var location in _spec.LocationsWithEmitters.ToArray()) {
                if (location.TryGetElement<LogicEmitterAction>(out var emitter)) {
                    emitter.ChangeState(_cyclicStates[_currentStateIndex].State);
                }
            }
            _nextActionTime = GetNextActionTime(currentTime);
            _nextIntervalEvent = new TimedEvent(_nextActionTime.Date, PerformActions);
            World.Any<GameTimeEvents>().AddEvent(_nextIntervalEvent);
        }

        int GetCurrentCyclicStateIndex(ARDateTime currentTime) {
            ARDateTime baseActionTime = GetDayTime(currentTime);
            int count = _cyclicStates.Count();
            for (int i = 0; i < count; i++) {
                var tempActionTime = baseActionTime + _cyclicStates[i].GetTime();
                if (tempActionTime > currentTime) {
                    if (i == 0) {
                        _currentStateIndex = count - 1;
                    } else {
                        _currentStateIndex =  i - 1;
                    }
                    return _currentStateIndex;
                }
            }
            _currentStateIndex = count - 1;
            return _currentStateIndex;
        }

        int GetNextCyclicStateIndex() {
            return (_currentStateIndex + 1) % _cyclicStates.Count();
        }

        ARDateTime GetNextActionTime(ARDateTime currentTime) {
            ARDateTime actionTime = GetDayTime(currentTime);
            actionTime += _cyclicStates[GetNextCyclicStateIndex()].GetTime();
            if (actionTime < currentTime) {
                actionTime += TimeSpan.FromDays(1);
            }
            return actionTime;
        }

        ARDateTime GetDayTime(ARDateTime currentTime) {
            return new ARDateTime(new DateTime(currentTime.Year, currentTime.Month, currentTime.DayOfTheMonth, 0, 0, 0));
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            World.Any<GameTimeEvents>()?.RemoveEvent(_nextIntervalEvent);
            _nextIntervalEvent = null;
        }
    }
}
