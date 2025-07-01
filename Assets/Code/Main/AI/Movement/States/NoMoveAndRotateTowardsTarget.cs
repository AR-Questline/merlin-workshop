using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class NoMoveAndRotateTowardsTarget : MovementState {
        bool _haveSetDestination;
        
        public override VelocityScheme VelocityScheme => VelocityScheme.NoMove;
        protected virtual IRotationScheme RotationScheme => new RotateTowardsCombatTarget();

        protected override void OnEnter() {
            _haveSetDestination = Controller.TrySetDestination(Controller.Position);
            Controller.SetRotationScheme(RotationScheme, VelocityScheme);
            Controller.FinalizeMovement();
        }
        protected override void OnExit() { }

        protected override void OnUpdate(float deltaTime) {
            if (!_haveSetDestination) {
                _haveSetDestination = Controller.TrySetDestination(Controller.Position);
            }
        }
    }
}