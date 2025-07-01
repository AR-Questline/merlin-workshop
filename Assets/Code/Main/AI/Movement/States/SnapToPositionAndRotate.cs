using Awaken.TG.Main.AI.Idle;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Maths;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class SnapToPositionAndRotate : MovementState {
        const float GroundedHeightAcceptableDifference = 0.125f;
        public const float DefaultSnapDuration = 0.5f;
        
        readonly RotateTowards _rotateTowards;
        readonly Vector3 _desiredForward;
        protected readonly Vector3 _desiredPosition;
        readonly GameObject _interactionGO;
        readonly float _snapDuration;
        
        protected float _snappingState;
        bool _reached;
        public override VelocityScheme VelocityScheme => VelocityScheme.NoMove;

        public SnapToPositionAndRotate(Vector3 position, Vector3 forward, 
            [CanBeNull] GameObject interactionGO, float snapDuration = DefaultSnapDuration) {
            _desiredPosition = position;
            _desiredForward = forward;
            _rotateTowards = new RotateTowards(forward);
            _interactionGO = interactionGO;
            _snapDuration = snapDuration;
        }
        
        protected override void OnEnter() {
            _reached = false;
            _snappingState = 0f;
            Controller.SetRotationScheme(_rotateTowards, VelocityScheme);
            Controller.FinalizeMovement();

            ValidateInteractionHeight();

            if (_snapDuration <= 0) {
                SnapInstantly();
            }
        }

        void SnapInstantly() {
            EndSnapping();
            Controller.SetForwardInstant(_desiredForward.ToHorizontal2());
        }

        void EndSnapping() {
            Controller.transform.position = _desiredPosition;
            Controller.TeleportTo(new TeleportDestination { position = _desiredPosition }, TeleportContext.SnapToPositionAndRotate);
            _reached = true;
        }

        void ValidateInteractionHeight() {
            if (!_interactionGO) {
                return;
            }

            bool isUsingInteractionPosition = _desiredPosition.EqualsApproximately(_interactionGO.transform.position, 0.05f);
            bool isHeightAcceptable = Mathf.Abs(_desiredPosition.y - Controller.Position.y) < GroundedHeightAcceptableDifference;

            if (isUsingInteractionPosition && !isHeightAcceptable) {
                Log.Minor?.Warning($"It seems that interaction {_interactionGO.PathInSceneHierarchy()} is too much above/below ground!", _interactionGO);
            }
        }

        protected override void OnUpdate(float deltaTime) {
            if (Controller.RotationScheme != _rotateTowards) {
                Controller.SetRotationScheme(_rotateTowards, VelocityScheme);
            }
            
            if (_reached) {
                return;
            }

            SnapStep(deltaTime);
        }

        protected virtual void SnapStep(float deltaTime) {
            var previousSnappingState = _snappingState;
            _snappingState += deltaTime / _snapDuration;
            
            if (_snappingState >= 1f) {
                EndSnapping();
                return;
            }
            
            PerformPositionEasingStep(previousSnappingState, _snappingState);
        }

        protected virtual void PerformPositionEasingStep(float currentEasingDelta, float targetEasingDelta) {
            float currentEasingValue = Easing.Cubic.InOut(currentEasingDelta);
            float targetEasingValue = Easing.Cubic.InOut(targetEasingDelta);
            float remainderDelta = Mathf.InverseLerp(currentEasingValue, 1f, targetEasingValue);
            
            Controller.transform.position = Vector3.Lerp(Controller.transform.position, _desiredPosition, remainderDelta);
        }
        
        protected override void OnExit() { }
    }
}