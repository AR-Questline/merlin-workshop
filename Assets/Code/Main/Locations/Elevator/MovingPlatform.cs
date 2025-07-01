using System;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Locations.Elevator {
    /// <summary>
    /// Marker for Locations that are on the moving platforms.
    /// </summary>
    public partial class MovingPlatform : Element<Location> {
        public sealed override bool IsNotSaved => true;

        readonly ElevatorPlatform _elevatorPlatform;
        TimeDependent _timeDependent;
        Action<float, ElevatorPlatform> _processFixedUpdate;
        Action<float, ElevatorPlatform> _processUpdate;
        
        public bool IsMoving => _elevatorPlatform.IsMoving;
        
        public new static class Events {
            public static readonly Event<Location, MovingPlatform> MovingPlatformAdded = new(nameof(MovingPlatformAdded));
            public static readonly Event<Location, MovingPlatform> MovingPlatformDiscarded = new(nameof(MovingPlatformDiscarded));
            public static readonly Event<MovingPlatform, bool> MovingPlatformStateChanged = new(nameof(MovingPlatformStateChanged));
        }
        
        public MovingPlatform(ElevatorPlatform elevatorPlatform) {
            _elevatorPlatform = elevatorPlatform;
        }

        protected override void OnFullyInitialized() {
            ParentModel.Trigger(Events.MovingPlatformAdded, this);
            _elevatorPlatform.ListenTo(ElevatorPlatform.Events.MovingStateChanged, OnMovingStateChanged, this);
            _timeDependent = this.GetOrCreateTimeDependent();

            if (_elevatorPlatform.IsMoving) {
                _timeDependent.WithUpdate(ProcessUpdate);
                _timeDependent.WithFixedUpdate(ProcessFixedUpdate);
                this.Trigger(Events.MovingPlatformStateChanged, true);
            }
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            ParentModel.Trigger(Events.MovingPlatformDiscarded, this);
            _processFixedUpdate = null;
            _processUpdate = null;
            _timeDependent?.WithoutFixedUpdate(ProcessFixedUpdate);
            base.OnDiscard(fromDomainDrop);
        }

        void ProcessFixedUpdate(float deltaTime) {
            _processFixedUpdate?.Invoke(deltaTime, _elevatorPlatform);
        }

        void ProcessUpdate(float deltaTime) {
            _processUpdate?.Invoke(deltaTime, _elevatorPlatform);
        }

        void OnMovingStateChanged(bool isMoving) {
            this.Trigger(Events.MovingPlatformStateChanged, isMoving);
            
            if (isMoving) {
                _timeDependent.WithUpdate(ProcessUpdate);
                _timeDependent.WithFixedUpdate(ProcessFixedUpdate);
            } else {
                _timeDependent.WithoutUpdate(ProcessUpdate);
                _timeDependent.WithoutFixedUpdate(ProcessFixedUpdate);
            }
        }

        public void WithFixedUpdate(Action<float, ElevatorPlatform> fixedUpdate) {
            _processFixedUpdate += fixedUpdate;
        }

        public void WithoutFixedUpdate(Action<float, ElevatorPlatform> fixedUpdate) {
            _processFixedUpdate -= fixedUpdate;
        }

        public void WithUpdate(Action<float, ElevatorPlatform> update) {
            _processUpdate += update;
        }

        public void WithoutUpdate(Action<float, ElevatorPlatform> update) {
            _processUpdate -= update;
        }
    }
}