using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class Wander : MovementState {
        CharacterPlace _destination;
        float? _instantExitRadiusSq;

        bool _haveSetDestination;

        readonly IRotationScheme _rotateTowards;
        readonly RotateTowardsMovement _toMovement = new();
        VelocityScheme _velocityScheme;

        public CharacterPlace Destination => _destination;
        public override VelocityScheme VelocityScheme => _velocityScheme;

        // === Creation
        public Wander(CharacterPlace place, VelocityScheme velocity, bool rotateTowardsTarget = false) {
            _rotateTowards = rotateTowardsTarget ? new RotateTowardsCombatTarget() : _toMovement;
            _destination = place;
            _velocityScheme = velocity;
        }

        // === State
        
        protected override void OnEnter() {
            if (_destination.Contains(Npc.Coords)) {
                End();
            } else {
                Controller.SetRotationScheme(Npc.Movement.Controller.ForwardMovementOnly ? _toMovement : _rotateTowards, VelocityScheme);
                _haveSetDestination = Controller.TrySetDestination(_destination.Position);
                Controller.onReached += End;
            }
        }

        protected override void OnExit() {
            if (Controller != null) {
                Controller.onReached -= End;
            }
        }

        protected override void OnUpdate(float deltaTime) {
            if (!_haveSetDestination) {
                _haveSetDestination = Controller.TrySetDestination(_destination.Position);
            }
            
            if (_instantExitRadiusSq is { } radiusSq 
                && _destination.DistanceSq(Npc.Coords) < radiusSq) {
                End();
            } else if (!Controller.RichAI.hasPath && !Controller.RichAI.pathPending && !Controller.RichAI.reachedEndOfPath) {
                // Edge case, no time to investigate the cause
                Controller.RichAI.SearchPath();
            }
        }
        
        public void UpdateDestination(CharacterPlace place) {
            _destination = place;
            _haveSetDestination = false;
            if (IsSetUp && ActiveSelf) {
                _haveSetDestination = Controller.TrySetDestination(_destination.Position);
            }
        }
        public void UpdateDestination(Vector3 place, float radius = 1) {
            UpdateDestination(new CharacterPlace(place, radius));
        }

        public void UpdateInstantExitRadiusSq(float? radiusSq) {
            _instantExitRadiusSq = radiusSq;
        }

        public void UpdateVelocityScheme(VelocityScheme velocityScheme) {
            _velocityScheme = velocityScheme;
        }
    }
}