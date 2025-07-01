using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Locations.Attachments;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    public partial class ElevatorPlatform : Element<Location>, IRefreshedByAttachment<ElevatorPlatformAttachment> {
        public override ushort TypeForSerialization => SavedModels.ElevatorPlatform;

        [Saved] Vector3 _savedPosition;
        
        float _speed;
        float _customDownwardsSpeed;
        Vector3 _targetPosition;
        GameObject _navmeshAddObject;
        ARFmodEventEmitter _elevatorEmitter;
        ARFmodEventEmitter _cogsEmitter;
        
        public Transform PlatformParentTransform { get; private set; }
        public Vector3 PositionChange { get; private set; }
        float CurrentSpeed { get; set; }
        [UnityEngine.Scripting.Preserve] public Vector3 Velocity => PositionChange / Time.fixedDeltaTime;

        public bool IsMoving { get; private set; }
        
        public new static class Events {
            public static readonly Event<ElevatorPlatform, ElevatorData> PlatformMoveRequested = new (nameof(PlatformMoveRequested));
            public static readonly Event<ElevatorPlatform, bool> MovingStateChanged = new (nameof(MovingStateChanged));
        }

        public void InitFromAttachment(ElevatorPlatformAttachment attachment, bool isRestored) {
            _speed = attachment.speed;
            _customDownwardsSpeed = attachment.useCustomDownwardsSpeed ? attachment.customDownwardsSpeed : attachment.speed;
            PlatformParentTransform = attachment.platformTransform;
            _navmeshAddObject = attachment.navmeshAddObject;
            _elevatorEmitter = attachment.elevatorEmitter;
            _cogsEmitter = attachment.cogsEmitter;
        }

        protected override void OnInitialize() {
            ParentModel.ViewParent = PlatformParentTransform;
        }

        protected override void OnRestore() {
            ParentModel.ViewParent = PlatformParentTransform;
            ParentModel.SafelyMoveTo(_savedPosition);
        }
        
        protected override void OnFullyInitialized() {
            this.ListenTo(Events.PlatformMoveRequested, StartMoving, this);
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            UnityUpdateProvider.TryGet()?.UnregisterElevatorPlatform(this);
        }
        
        public void FixedUpdate(float fixedDeltaTime) {
            if (!IsMoving) {
                return;
            }

            Vector3 platformPosition = Vector3.MoveTowards(ParentModel.Coords, _targetPosition, CurrentSpeed * fixedDeltaTime);
            if (platformPosition.Equals(_targetPosition)) {
                StopMoving();
                return;
            }
            
            PositionChange = platformPosition - ParentModel.Coords;
            ParentModel.SafelyMoveTo(platformPosition);
        }

        void StopMoving() {
            PositionChange = Vector3.zero;
            ParentModel.SafelyMoveTo(_targetPosition);
            
            IsMoving = false;
            this.Trigger(Events.MovingStateChanged, false);
            TryToSetActiveNavMeshAddObject(false);
            SetActiveAudioEmitters(false);
            UnityUpdateProvider.TryGet()?.UnregisterElevatorPlatform(this);
        }

        void StartMoving(ElevatorData data) {
            if (IsMoving || _targetPosition == data.targetPoint) {
                return;
            }
            
            _targetPosition = data.targetPoint;
            CurrentSpeed = _targetPosition.y < ParentModel.Coords.y ? _customDownwardsSpeed : _speed;

            if (data.instant) {
                ParentModel.SafelyMoveTo(_targetPosition);
                return;
            }

            IsMoving = true;
            this.Trigger(Events.MovingStateChanged, true);
            TryToSetActiveNavMeshAddObject(true);
            SetActiveAudioEmitters(true);
            UnityUpdateProvider.GetOrCreate().RegisterElevatorPlatform(this);
        }
        
        void TryToSetActiveNavMeshAddObject(bool active) {
            if (_navmeshAddObject != null) {
                _navmeshAddObject.SetActive(active);
            }
        }

        protected override bool OnSave() {
            _savedPosition = ParentModel.Coords;
            return true;
        }

        void SetActiveAudioEmitters(bool active) {
            if (_elevatorEmitter != null) {
                if (active) {
                    //_elevatorEmitter.Play();
                } else {
                    //_elevatorEmitter.Stop();
                }
            }

            if (_cogsEmitter != null) {
                if (active) {
                    // _cogsEmitter.Play();
                } else {
                    // _cogsEmitter.Stop();
                }
            }
        }
    }
}