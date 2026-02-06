using System.Collections.Generic;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Utility.Debugging;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    public partial class ElevatorGroups : LogicEmitterActionBase<ElevatorGroupsAttachment>, UnityUpdateProvider.IWithUpdateGeneric {
        public override ushort TypeForSerialization => SavedModels.ElevatorGroups;

        bool _working;
        [Saved] float _currentTimer = -1f;
        [Saved] int _cycleRepeatsDone = 0;
        ElevatorLeverAction[] _levers;
        
        protected override IEnumerable<Location> Locations => _locations ??= CollectLocations();

        protected override void OnLateInit() {
            DelayLateInit().Forget();
        }

        async UniTaskVoid DelayLateInit() {
            if (!await AsyncUtil.DelayFrame(this)) {
                return;
            }
            if (_currentTimer < 0f || _currentTimer >= _attachment.cycleDuration) {
                _currentTimer = -1f;
                _cycleRepeatsDone = 0;
                SetDefaultState(false);
            } else {
                SetState(_currentTimer);
                StartGroupUpdate(_currentTimer);
            }
        }
        
        protected override bool IsActive() => _currentTimer < 0f;
        
        protected override void SendInteractEventsToLocation(Location location, bool active) {
            if (active == _working) {
                return;
            }

            if (active) {
                StartGroupUpdate(0f);
            } else {
                StopGroupUpdate();
            }
        }

        Location[] CollectLocations() {
            var availableLocations = World.All<Location>();
            var locations = new Location[_attachment.elevatorCycleData.Length];
            _levers = new ElevatorLeverAction[locations.Length];
            for (int i = 0; i < _attachment.elevatorCycleData.Length; i++) {
                var spec = _attachment.elevatorCycleData[i].locationSpec;
                if (spec == null) {
                    Log.Critical?.Error($"{LogUtils.GetDebugName(this)} has no Location Spec with lever assigned for index {i}.");
                    continue;
                }
                locations[i] = availableLocations.FirstOrDefault(location => location.Spec == spec);
                if (locations[i] == null) {
                    Log.Critical?.Error($"{LogUtils.GetDebugName(this)} has unexisting Location assigned for index {i}.");
                    continue;
                }
                _levers[i] = locations[i].TryGetElement<ElevatorLeverAction>();
                if (_levers[i] == null) {
                    Log.Critical?.Error($"{LogUtils.GetDebugName(this)} has Location without ElevatorLeverAction assigned for index {i}.");
                    continue;
                }
            }
            return locations;
        }

        void StartGroupUpdate(float timer) {
            _working = true;
            _currentTimer = timer;
            UnityUpdateProvider.GetOrCreate().RegisterGeneric(this);
        }

        public void UnityUpdate() {
            _currentTimer += Time.deltaTime;
            SetState(_currentTimer);
            if (_currentTimer >= _attachment.cycleDuration) {
                _cycleRepeatsDone++;
                if (_attachment.cycleRepeats < _cycleRepeatsDone) {
                    StopGroupUpdate();
                } else {
                    _currentTimer -= _attachment.cycleDuration;
                }
            }
        }

        void StopGroupUpdate() {
            UnityUpdateProvider.TryGet()?.UnregisterGeneric(this);
            _currentTimer = -1f;
            _cycleRepeatsDone = 0;
            SetDefaultState(true);
            _working = false;
        }

        void SetState(float timer) {
            for (int i = 0; i < _attachment.elevatorCycleData.Length; i ++) {
                int indexToSet = _attachment.elevatorCycleData[i].states.Length - 1; 
                for (int j = 0; j < _attachment.elevatorCycleData[i].states.Length; j++) {
                    if (_attachment.elevatorCycleData[i].states[j].time > timer) {
                        break;
                    }
                    indexToSet = j;
                }
                int targetFloor = _cycleRepeatsDone == 0
                    ? _attachment.elevatorCycleData[i].states[indexToSet].TargetFloorAtFirstCycle
                    : _attachment.elevatorCycleData[i].states[indexToSet].targetFloor;
                SetFloor(i, targetFloor);
            }
        }

        void SetDefaultState(bool withDelay) {
            for (int i = 0; i < _attachment.elevatorCycleData.Length; i ++) {
                if (withDelay && _attachment.elevatorCycleData[i].delayReset > 0f) {
                    SetDefaultStateWithDelay(i, _attachment.elevatorCycleData[i].defaultFloor, _attachment.elevatorCycleData[i].delayReset).Forget();
                } else {
                    SetFloor(i, _attachment.elevatorCycleData[i].defaultFloor);
                }
            }
        }

        async UniTaskVoid SetDefaultStateWithDelay(int index, int floor, float delay) {
            if (!await AsyncUtil.DelayTime(this, delay)) {
                return;
            }
            SetFloor(index, floor);
        }

        void SetFloor(int index, int floor) {
            if (_levers[index] is not { HasBeenDiscarded: false }) {
                return;
            }
            if (_levers[index].CurrentIndex != floor) {
                ElevatorPlatform platform = _levers[index].Owner;
                if (platform is not { HasBeenDiscarded: false }) {
                    return;
                }
                _levers[index].RequestMoveToCaller(_levers[index].Owner, floor);
            }
        }
    }
}