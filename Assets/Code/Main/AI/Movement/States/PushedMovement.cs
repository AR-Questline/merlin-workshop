using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.Controllers.Rotation;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Movement.States {
    public class PushedMovement : MovementState {
        const float MaxDistance = 7f;
        const float WaitDuration = 5f;
        
        Vector3 _startingPosition;
        Force _forceToApply;
        float _forceMultiplier;
        float _duration;
        float _timePassed;
        bool _reached;
        VelocityScheme _velocityScheme;
        INpcInteraction _pausedInteraction;
        public override VelocityScheme VelocityScheme => _velocityScheme;

        public PushedMovement(Force forceToApply, VelocityScheme velocityScheme) {
            _forceToApply = forceToApply;
            _forceMultiplier = 1f;
            _velocityScheme = velocityScheme;
            _duration = forceToApply.duration;
            OnEnd += ExitPush;
        }

        public void Update(Force forceToApply) {
            float distanceFromStart = (Npc.Coords - _startingPosition).magnitude;
            if (distanceFromStart > MaxDistance) {
                return;
            }
            
            _forceToApply = forceToApply;
            _forceMultiplier *= 1f - distanceFromStart/MaxDistance;
            _duration = forceToApply.duration;
            _timePassed = 0;
            _reached = false;
            OnEnter();
        }
        
        protected override void OnEnter() {
            _startingPosition = Npc.Coords;
            Controller.SetRotationScheme(new NoRotationChange(), VelocityScheme);
            if (Npc.Interactor.CurrentInteraction is { CanBeInterrupted: true, CanBePushedFrom: true } interaction) {
                _pausedInteraction = interaction;
                Npc.Interactor.Stop(InteractionStopReason.StoppedIdlingInstant, true);
            }
        }

        protected override void OnExit() { }

        protected override void OnUpdate(float deltaTime) {
            if (_reached) {
                return;
            }

            if (_timePassed < _duration) {
                UpdatePosition(deltaTime);
            } else if (_timePassed > (_duration + WaitDuration)) {
                ExitPush();
                return;
            }

            _timePassed += deltaTime;
        }

        void ExitPush() {
            _reached = true;
            Movement.StopInterrupting();
            if (_pausedInteraction != null) {
                Npc.Interactor.TryToPerformAgain(_pausedInteraction, false, out _);
            } else {
                Npc.Behaviours.RefreshCurrentBehaviour();
            }
        }
        
        void UpdatePosition(float deltaTime) {
            Vector3 force = _forceToApply.direction.ToHorizontal3() * (deltaTime * _forceMultiplier);
            if (Controller.RichAI.hasPath) {
                Controller.Move(force, false, true);
            } else {
                Controller.TrySetDestination(Controller.Position + force);
            }
        }

        public static bool CanNpcBePushed(NpcElement npc) {
            return npc.Interactor.CurrentInteraction is not { CanBeInterrupted: false } and not { CanBePushedFrom: false };
        }
    }
}