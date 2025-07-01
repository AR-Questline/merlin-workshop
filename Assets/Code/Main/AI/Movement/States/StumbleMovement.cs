using System;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Utility;
using Pathfinding;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class StumbleMovement : MovementState {
        const float StumbledStumbleDuration = 0.8f;
        const float StumbleMoveForce = 1.0f;
        const float FallCheckInterval = 0.1f;
        const float FallCheckOffset = 0.6f;
        const float FallRecoveryMaxHorizontalDistance = 0.3f;
        const float FallRecoveryMaxVerticalDistance = 0.6f;
        const float AdditionalRagdollDuration = 1.5f;
        const float RagdollEasingDuration = 0.3f;
        
        static readonly NNConstraint NNConstraintClosestAsSeenFromAbove = new() {
            constrainWalkability = false,
            constrainTags = false,
            constrainDistance = true,
            distanceMetric = DistanceMetric.ClosestAsSeenFromAbove(),
        };

        public Action<RagdollMovement> exitToRagdoll;
        readonly Vector3 _forceDirection;
        readonly bool _stumbled;
        readonly bool _isPush;
        readonly float _duration;
        readonly float _ragdollForce;
        float _timePassed;
        int _fallChecks;
        bool _reached;
        Vector3 _lastCheckStepPosition;

        bool CanRagdoll => _isPush || _stumbled;
        public override VelocityScheme VelocityScheme => VelocityScheme.NoMove;

        public StumbleMovement(Force forceToApply, bool stumbled, bool isPush) {
            _forceDirection = forceToApply.direction.ToHorizontal3().normalized;
            _ragdollForce = forceToApply.direction.magnitude;
            _stumbled = stumbled;
            _isPush = isPush;
            _duration = forceToApply.duration;
            
            OnEnd += ExitStumble;
        }
        
        protected override void OnEnter() {
            _fallChecks = 0;
            _timePassed = 0f;
            
            if (_stumbled) {
                Controller.SetForwardInstant(_forceDirection * -1);
                Controller.SetRotationScheme(new NoRotationChange(), VelocityScheme);
            }
        }

        protected override void OnExit() { }

        protected override void OnUpdate(float deltaTime) {
            if (_reached) {
                return;
            }

            float stateDuration = _stumbled ? StumbledStumbleDuration : _duration;
            if (_timePassed > stateDuration) {
                ExitStumble();
                return;
            }

            UpdatePosition(deltaTime);
            
            _timePassed += deltaTime;
        }

        void ExitStumble() {
            _reached = true;
            Movement.StopInterrupting();
        }
        
        void UpdatePosition(float deltaTime) {
            if (_forceDirection == Vector3.zero) {
                return;
            }
            
            StumbleFixedMove(deltaTime);
            TryNextRagdollCheckStep();
        }

        void StumbleFixedMove(float deltaTime) {
            if (_timePassed >= _duration) {
                return;
            }
            
            Vector3 movementDelta = _forceDirection * StumbleMoveForce * deltaTime;
            Controller.Move(movementDelta, false, false);
        }

        void TryNextRagdollCheckStep() {
            if (!CanRagdoll) {
                return;
            }
            
            bool shouldDoNextStep = _timePassed / FallCheckInterval >= _fallChecks;
            if (shouldDoNextStep) {
                PerformRagdollCheckStep();
                _fallChecks++;
            }
        }

        void PerformRagdollCheckStep() {
            var afterPushPosition = FindFallCheckPosition();

            if (WillFallFromCliffAt(afterPushPosition)) {
                PerformRagdoll();
            }
            
            _lastCheckStepPosition = Npc.Coords;
        }

        Vector3 FindFallCheckPosition() {
            var afterPushPosition = Npc.Coords + GetFallEdgeDirection() * FallCheckOffset;
            return afterPushPosition;
        }

        Vector3 GetFallEdgeDirection() {
            if (_fallChecks == 0) {
                return _forceDirection;
            }

            Vector3 movedDelta = (Npc.Coords - _lastCheckStepPosition).ToHorizontal3();
            if (movedDelta == Vector3.zero) {
                return _forceDirection;
            }
            
            Vector3 movedDirection = (Npc.Coords - _lastCheckStepPosition).ToHorizontal3().normalized;
            float movingToEdgeFactor = Vector3.Dot(movedDirection, _forceDirection);
            if (movingToEdgeFactor >= 0.7f) {
                return _forceDirection;
            }

            Vector3 verticalComponent = Vector3.Cross(_forceDirection, movedDirection);
            Vector3 edgeDirection = Vector3.Cross(movedDirection, verticalComponent);
            return edgeDirection;
        }

        bool WillFallFromCliffAt(Vector3 searchPosition) {
            NNInfo nearest = AstarPath.active.GetNearest(searchPosition, NNConstraintClosestAsSeenFromAbove);

            if (nearest.node == null) {
                return true;
            }
            
            float horizontalDistance = (searchPosition - nearest.position).ToHorizontal2().magnitude;
            float verticalFallDistance = searchPosition.y - nearest.position.y;
            
            bool isNearestLowEnough = verticalFallDistance > FallRecoveryMaxVerticalDistance;
            bool isNearestFarEnough =  horizontalDistance >= FallRecoveryMaxHorizontalDistance;
            
            return isNearestLowEnough || isNearestFarEnough;
        }

        void PerformRagdoll() {
            float ragdollDuration = (StumbledStumbleDuration - _timePassed) + AdditionalRagdollDuration;
            exitToRagdoll?.Invoke(new RagdollMovement(_forceDirection, GetRagdollForceToApply(), ragdollDuration));
        }

        float GetRagdollForceToApply() {
            float forceFromDelta = GetRagdollForceFromDelta();
            return Mathf.Lerp(_ragdollForce, forceFromDelta, _timePassed / RagdollEasingDuration);
        }

        float GetRagdollForceFromDelta() {
            if (_fallChecks == 0) {
                return _ragdollForce;
            }

            Vector3 movementDelta = (Npc.Coords - _lastCheckStepPosition).ToHorizontal3();
            float velocity = movementDelta.magnitude / FallCheckInterval;
            float mass = Npc.Template.npcWeight;

            return velocity * mass;
        }
    }
}