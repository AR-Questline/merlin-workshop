using System;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs.Providers;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class KeepPosition : MovementState, IDesiredDistanceToTargetProvider {
        public const float DefaultMaxStrafeDistance = 8f;
        const float ProlongChaseDelay = 2.5f;
        
        bool _seekingNewPath;
        CharacterPlace _place;

        bool _haveSetDestination;
        float _targetFallingBackTimer;
        float _defaultDesiredDistance;
 
        protected readonly VelocityScheme closeVelocity;
        protected readonly VelocityScheme chaseVelocity;
        readonly RotateTowardsMovement _rotateToMovement;
        readonly RotateTowardsCombatTarget _rotateToTarget;
        readonly float _chaseDistanceSq;
        readonly float _maxStrafeDistanceSq;
        readonly bool _invertRotation;

        public event Action OnReached;
        public event Action<VelocityScheme> VelocityChanged;
        public override VelocityScheme VelocityScheme => _velocityScheme;
        public float DesiredDistanceToTarget => math.max(1, TargetFallingBack ? _defaultDesiredDistance * 0.25f : _defaultDesiredDistance);
        bool TargetFallingBack => _targetFallingBackTimer > 0;
        VelocityScheme _velocityScheme;

        public KeepPosition(CharacterPlace place, VelocityScheme closeVelocity, float maxStrafeDistance, float distanceToChase, VelocityScheme chaseVelocity, bool invertRotation = false) {
            _place = place;
            _velocityScheme = closeVelocity;
            this.closeVelocity = closeVelocity;
            this.chaseVelocity = chaseVelocity;
            _rotateToMovement = new RotateTowardsMovement();
            _rotateToTarget = new RotateTowardsCombatTarget();
            _chaseDistanceSq = distanceToChase * distanceToChase;
            _maxStrafeDistanceSq = maxStrafeDistance * maxStrafeDistance;
            _invertRotation = invertRotation;
        }
        
        public KeepPosition(CharacterPlace place, VelocityScheme velocity, float maxStrafeDistance = KeepPosition.DefaultMaxStrafeDistance) {
            _place = place;
            _velocityScheme = velocity;
            _rotateToMovement = new RotateTowardsMovement();
            _rotateToTarget = new RotateTowardsCombatTarget();
            _chaseDistanceSq = float.MaxValue;
            _maxStrafeDistanceSq = maxStrafeDistance * maxStrafeDistance;
            closeVelocity = velocity;
            chaseVelocity = velocity;
        }

        protected override void OnEnter() {
            Controller.SetRotationScheme(Controller.ForwardMovementOnly ? _rotateToMovement : _rotateToTarget, VelocityScheme);
            _haveSetDestination = Controller.TrySetDestination(_place.Position);
            Controller.onReached += OnReached;
            _defaultDesiredDistance = Npc.DefaultDesiredDistanceToTarget;
            DistancesToTargetHandler.AddDesiredDistanceToTargetProvider(Npc, this);
        }

        protected override void OnExit() {
            Controller.onReached -= OnReached;
            DistancesToTargetHandler.RemoveDesiredDistanceToTargetProvider(Npc, this);
        }

        protected override void OnUpdate(float deltaTime) {
            Vector3 targetPosition = Controller.Npc.GetCurrentTarget()?.Coords ?? _place.Position;
            float distanceToTargetSqr = (Controller.Position - targetPosition).sqrMagnitude;
            bool inDistanceToChase = distanceToTargetSqr > _chaseDistanceSq;
            bool inDistanceToStrafe = distanceToTargetSqr <= _maxStrafeDistanceSq;
            if (Controller.Npc.TryGetCurrentTarget(out ICharacter target) && target.RelativeForwardVelocity < 0) {
                _targetFallingBackTimer = ProlongChaseDelay;
            } else if (_targetFallingBackTimer >= 0) {
                _targetFallingBackTimer -= deltaTime;
            }
            
            var previousVelocityScheme = _velocityScheme;
            _velocityScheme = DetermineVelocityScheme(inDistanceToChase);

            if (previousVelocityScheme != _velocityScheme) {
                VelocityChanged?.Invoke(_velocityScheme);
            }

            IRotationScheme newRotationScheme = Controller.ForwardMovementOnly
                ? _rotateToMovement
                : inDistanceToStrafe
                    ? (_invertRotation ? _rotateToMovement : _rotateToTarget)
                    : (_invertRotation ? _rotateToTarget : _rotateToMovement);
            Controller.SetRotationScheme(newRotationScheme, _velocityScheme);

            if (!_haveSetDestination) {
                _haveSetDestination = Controller.TrySetDestination(_place.Position);
            }
        }

        protected virtual VelocityScheme DetermineVelocityScheme(bool inDistanceToChase) {
            if (TargetFallingBack) {
                return VelocityScheme.Run;
            }

            return inDistanceToChase ? chaseVelocity : closeVelocity;
        }
        
        public void UpdatePlace(CharacterPlace place) {
            _place = place;
            _haveSetDestination = false;
            if (IsSetUp && ActiveSelf) {
                _haveSetDestination = Controller.TrySetDestination(place.Position);
            }
        }

        public void UpdatePlace(Vector3 place, float radius) {
            UpdatePlace(new CharacterPlace(place, radius));
        }
    }
}