using System;
using System.Collections.Generic;
using Awaken.TG.Main.Locations.Actions.Attachments;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    public partial class ElevatorLeverAction : LogicEmitterActionBase<ElevatorLeverAttachment> {
        public override ushort TypeForSerialization => SavedModels.ElevatorLeverAction;

        protected static readonly int InvalidOpHash = Animator.StringToHash("InvalidOpHash");
        
        [Saved] int _currentIndex;
        [Saved] bool _currentState;
        List<WeakModelRef<ElevatorCallerAction>> _platformCallers;
        WeakModelRef<ElevatorPlatform> _owner;

        protected override void OnLateInit() {
            _platformCallers = new List<WeakModelRef<ElevatorCallerAction>>();
            var availableLocations = World.All<Location>();
            
            //assign platform callers in provided order - there can be duplicates
            foreach (LocationSpec locationSpec in _attachment.locationSpecsReferences.WhereNotUnityNull()) {
                LocationSpec spec = locationSpec;
                Location specLocation = availableLocations.FirstOrDefault(location => location.Spec == spec);
                ElevatorCallerAction elevatorCaller = specLocation.Element<ElevatorCallerAction>();

                //listen to platform caller but only if it's not already listened to
                if (!_platformCallers.Contains(elevatorCaller)) {
                    elevatorCaller.ListenTo(ElevatorCallerAction.Events.PlatformCalled, OnPlatformCalled, this);
                    elevatorCaller.ListenTo(Events.AfterDiscarded, AfterCallerDiscarded, this);
                }

                _platformCallers.Add(new WeakModelRef<ElevatorCallerAction>(elevatorCaller));
            }

            foreach (Location location in Locations) {
                var ep = location.TryGetElement<ElevatorPlatform>();
                if (ep) {
                    // Lever toggling based on platform movement
                    ep.ListenTo(ElevatorPlatform.Events.MovingStateChanged, OnMovingStateChanged, this);
                    _owner = ep;
                    break;
                }
            }
            
            MoveToInitialFloor();
        }

        void OnMovingStateChanged(bool state) {
            _animator?.SetBool(ActiveHash, state);
            HandleFakeFloorNavmeshCuts();
        }

        void MoveToInitialFloor() {
            _owner.Get()?.Trigger(ElevatorPlatform.Events.PlatformMoveRequested, new ElevatorData(_platformCallers[_currentIndex].Get().TargetPosition));
        }

        void OnPlatformCalled(ElevatorCallerAction elevatorCaller) {
            if (elevatorCaller == _platformCallers[_currentIndex].Get()) {
                return;
            }
            
            //find next platform caller
            
            int nextIndex = _platformCallers.IndexOf(elevatorCaller, (_currentIndex + 1) % _platformCallers.Count);
            if (nextIndex == -1) {
                nextIndex = _platformCallers.IndexOf(elevatorCaller);
                if (nextIndex == -1) {
                    Log.Important?.Error($"Platform caller: {elevatorCaller} cannot be found on the platform callers list");
                    return;
                }
            }
            _currentIndex = nextIndex;
        }

        void HandleFakeFloorNavmeshCuts() {
            bool isPlatformMoving = _owner.Get()?.IsMoving ?? true;
            for (int i = 0; i < _platformCallers.Count; i++) {
                if (isPlatformMoving) {
                    _platformCallers[i].Get().SetNavmeshCutObjectActive(true);
                } else {
                    _platformCallers[i].Get().SetNavmeshCutObjectActive(i != _currentIndex);
                }
                
            }
        }

        void AfterCallerDiscarded(Model _) {
            _platformCallers.RemoveAll(c => !c.Exists());
        }

        protected override bool IsActive() => ParentModel.Interactability.interactable && !(_owner.Get()?.IsMoving ?? true);

        protected override void SendInteractEventsToLocation(Location location, bool active) {
            ElevatorPlatform platform = location.TryGetElement<ElevatorPlatform>();
            if (platform is {IsMoving: true}) {
                return;
            }
            
            _currentIndex = (_currentIndex + 1) % _platformCallers.Count;
            _platformCallers[_currentIndex].Get().ParentModel.SetInteractability(LocationInteractability.Active);
            platform.Trigger(ElevatorPlatform.Events.PlatformMoveRequested, new ElevatorData(_platformCallers[_currentIndex].Get().TargetPosition));
        }

        protected override void OnAnimatorUpdate(bool action) {
            if (!action) {
                _animator.SetTrigger(InvalidOpHash);
            }
        }
    }
}