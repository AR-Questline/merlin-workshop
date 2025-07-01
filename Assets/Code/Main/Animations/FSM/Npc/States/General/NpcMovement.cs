using Awaken.Utility;
using System;
using Animancer;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Utility.Animations.ARTransitions;
using UnityEngine;
using Unity.Mathematics;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcMovement : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcMovement;

        const float DegreesPerSecondPerMixerUnit = 90.0f;
        const float MinTimeForTurningInIdleOverride = 0f;
        const float MinTimeForTurningOutsideIdleOverride = 0.25f;
        const float BlendTimeInDialogue = 0.5f;
        public override NpcStateType Type => NpcStateType.Movement;
        public override bool CanOverrideDestination => false;
        protected override NpcStateType StateToEnter => GetCurrentMovementMovementState switch {
            NpcMovementState.Combat => NpcStateType.CombatMovement,
            NpcMovementState.Alert => NpcStateType.AlertMovement,
            NpcMovementState.Idle => NpcStateType.Movement,
            NpcMovementState.Fear => NpcStateType.FearMovement,
            _ => throw new ArgumentOutOfRangeException()
        };
        public override bool CanReEnter => true;

        protected virtual bool CanLeaveToIdle => true;
        protected virtual NpcStateType TurningState => NpcStateType.TurnMovement;
        
        MixerState<Vector2> _mixerState;
        NpcMovementState _lastMovementState;

        bool CanStartTurningOverride => 
            CurrentState.Time > MinTimeForTurningOverride &&
            Npc.Interactor.CurrentInteraction is not { CanUseTurnMovement: false };

        float MinTimeForTurningOverride => _lastMovementState == NpcMovementState.Idle ? MinTimeForTurningInIdleOverride : MinTimeForTurningOutsideIdleOverride;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _mixerState = (MixerState<Vector2>)CurrentState;
            _lastMovementState = GetCurrentMovementMovementState;
        }

        protected override void OnUpdate(float deltaTime) {
            if (CanLeaveToIdle && IsMostlyStationary()) {
                ParentModel.SetCurrentState(NpcStateType.Idle, ParentModel.ParentModel.IsInDialogue ? BlendTimeInDialogue : null);

                return;
            }

            UpdateMixerParameters();

            if (MovementTypeChanged()) {
                ParentModel.SetCurrentState(Type);
                return;
            }
            
            if (CanStartTurningOverride && HasTurningOverrideAvailable()) {
                ParentModel.SetCurrentState(TurningState);
            }
        }

        void UpdateMixerParameters() {
            if (_mixerState == null) {
                return;
            }

            var movementType = ARMixerTransition.MovementType.Strafing;
                
            if (_mixerState is IARMixerState arMixerState) {
                movementType = arMixerState.Properties.movementType;
            }

            float horizontalAxis = movementType switch {
                ARMixerTransition.MovementType.Strafing => NpcAnimancer.VelocityHorizontal,
                ARMixerTransition.MovementType.Turning => NpcAnimancer.AngularVelocity / DegreesPerSecondPerMixerUnit,
                _ => 0,
            };
            
            _mixerState.Parameter = new Vector2(horizontalAxis, NpcAnimancer.VelocityForward);
        }

        protected override void OnExit(bool restarted) {
            _mixerState = null;
            base.OnExit(restarted);
        }

        public override void OnAnimancerUnload() {
            _mixerState = null;
            base.OnAnimancerUnload();
        }

        // === Helpers
        bool IsMostlyStationary() =>
            NpcAnimancer.MovementSpeed < 0.05f && math.abs(NpcAnimancer.AngularVelocity) < 0.05f;
        
        bool MovementTypeChanged() => 
            _lastMovementState != GetCurrentMovementMovementState;
        bool HasTurningOverrideAvailable() => 
            _mixerState is IARMixerState arMixerState && arMixerState.Properties.turningOverrides.ShouldOverrideFor(Npc);
    }
}