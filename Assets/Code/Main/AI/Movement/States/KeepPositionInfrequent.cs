using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class KeepPositionInfrequent : MovementState {
        readonly float _escapeFromTargetAtDistance;
        readonly RotateTowardsMovement _rotateToMovement;
        readonly RotateTowardsCombatTarget _rotateToTarget;
        readonly VelocityScheme _velocityScheme;

        bool _haveSetDestination;
        float _lastPositionUpdate;
        CharacterPlace _place;

        public override VelocityScheme VelocityScheme => _velocityScheme;

        public KeepPositionInfrequent(VelocityScheme velocityScheme, CharacterPlace place, float escapeFromTargetAtDistance) {
            _velocityScheme = velocityScheme;
            _place = place;
            _escapeFromTargetAtDistance = escapeFromTargetAtDistance;
            
            _rotateToMovement = new RotateTowardsMovement();
            _rotateToTarget = new RotateTowardsCombatTarget();
        }

        protected override void OnEnter() {
            Controller.SetRotationScheme(Controller.ForwardMovementOnly ? _rotateToMovement : _rotateToTarget, VelocityScheme);
            SetDestination();
        }

        protected override void OnExit() { }

        protected override void OnUpdate(float deltaTime) {
            ICharacter currentTarget = Controller.Npc.GetCurrentTarget();
            if (currentTarget != null) {
                Vector3 targetPosition = currentTarget.Coords;
                if ((targetPosition - Npc.Coords).sqrMagnitude <= _escapeFromTargetAtDistance) {
                    SetDestination();
                    return;
                }
            }
            
            if (!_haveSetDestination && Time.time > _lastPositionUpdate + 5f) {
                SetDestination();
            }
        }

        void SetDestination() {
            if (!IsSetUp || !ActiveSelf) {
                return;
            }
            
            _haveSetDestination = Controller.TrySetDestination(_place.Position);
            if (_haveSetDestination) {
                _lastPositionUpdate = Time.time;
            }
        }

        public void UpdatePlace(CharacterPlace place) {
            _place = place;
            _haveSetDestination = false;
        }

        [UnityEngine.Scripting.Preserve]
        public void UpdatePlace(Vector3 place, float radius) {
            UpdatePlace(new CharacterPlace(place, radius));
        }
    }
}