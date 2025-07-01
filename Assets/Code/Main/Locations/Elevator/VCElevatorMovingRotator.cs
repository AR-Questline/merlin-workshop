using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Elevator {
    public class VCElevatorMovingRotator : ViewComponent<Location> {
        [SerializeField] float rotSpeed = 180;
        [SerializeField] Axis axis;
        bool _rotate;
        Vector3 _rotationAxis;
        Transform _transform;
        
        protected override void OnAttach() {
            Target.AfterFullyInitialized(Init);
        }

        void Init() {
            var elevatorPlatform = Target.TryGetElement<ElevatorPlatform>();
            if (elevatorPlatform == null) return;

            _rotationAxis = GetBaseAxis(axis);
            _transform = transform;
            elevatorPlatform.ListenTo(ElevatorPlatform.Events.MovingStateChanged, PlatformMovementChanged, this);
            Target.GetOrCreateTimeDependent().WithUpdate(RotationUpdate);
        }

        void RotationUpdate(float deltaTime) {
            if (!_rotate) return;
            
            _transform.Rotate(_rotationAxis, rotSpeed * deltaTime);
        }

        void PlatformMovementChanged(bool elevatorState) {
            _rotate = elevatorState;
        }

        protected override void OnDiscard() {
            Target.GetTimeDependent()?.WithoutUpdate(RotationUpdate);
        }
        
        static Vector3 GetBaseAxis(Axis axis) {
            return axis switch {
                Axis.X => new Vector3(1, 0, 0),
                Axis.Y => new Vector3(0, 1, 0),
                Axis.Z => new Vector3(0, 0, 1),
                _ => Vector3.zero
            };
        }
        
        enum Axis : byte {
            X = 0,
            Y = 1,
            Z = 2
        }
    }
}