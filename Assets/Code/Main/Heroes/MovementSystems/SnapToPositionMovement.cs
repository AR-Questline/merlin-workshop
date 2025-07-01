using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.MovementSystems {
    public partial class SnapToPositionMovement : HeroMovementSystem {
        public override ushort TypeForSerialization => SavedModels.SnapToPositionMovement;

        const float SnappingSpeed = 2;
        Vector3 _desiredPosition;
        
        public override MovementType Type => MovementType.SnapToPosition;
        public override bool CanCurrentlyBeOverriden => false;
        public override bool RequirementsFulfilled => true;
        CharacterController CController { get; set; }

        // === Init
        protected override void Init() {
            CController = Controller.GetComponent<CharacterController>();
        }
        protected override void SetupForceExitConditions() { }
        
        // === Public API
        public void AssignDesiredPosition(Vector3 position) {
            _desiredPosition = position;
        }
        
        // === Update
        public override void Update(float deltaTime) {
            if ((Hero.Coords - _desiredPosition).sqrMagnitude < 0.1f) {
                return;
            }

            Vector3 moveDirection = (_desiredPosition - Hero.Coords);
            float magnitude = moveDirection.magnitude;
            float snapDistance = deltaTime * SnappingSpeed;
            if (magnitude > snapDistance) {
                moveDirection = moveDirection.normalized * snapDistance;
            }
            CController.Move(moveDirection);
            ParentModel.MoveTo(Controller.transform.position);
        }

        public override void FixedUpdate(float deltaTime) { }
    }
}