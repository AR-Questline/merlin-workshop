using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class NoMove : MovementState {
        readonly NoRotationChange _noRotationChange = new();
        Vector3 _initialPosition;
        
        bool _haveSetDestination;
        
        public override VelocityScheme VelocityScheme => VelocityScheme.NoMove;

        protected override void OnEnter() {
            _haveSetDestination = Controller.TrySetDestination(Controller.Position);
            Controller.SetRotationScheme(_noRotationChange, VelocityScheme);
            Controller.FinalizeMovement();
        }

        protected override void OnUpdate(float deltaTime) {
            if (!_haveSetDestination) {
                _haveSetDestination = Controller.TrySetDestination(Controller.Position);
            }
        }
        protected override void OnExit() { }
    }
}