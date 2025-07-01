using Awaken.Utility;
using System;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Unity.Mathematics;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcIdle : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcIdle;

        const float BlendDuration = 0.75f;
        
        public override NpcStateType Type => NpcStateType.Idle;
        public override bool CanOverrideDestination => false;
        protected override NpcStateType StateToEnter => GetCurrentMovementMovementState switch {
            NpcMovementState.Combat => NpcStateType.CombatIdle,
            NpcMovementState.Alert => NpcStateType.AlertIdle,
            NpcMovementState.Idle => Npc.IsInDialogue ? NpcStateType.DialogueIdle : NpcStateType.Idle,
            NpcMovementState.Fear => NpcStateType.FearIdle,
            _ => throw new ArgumentOutOfRangeException()
        };
        public override bool CanReEnter => true;
        NpcStateType _lastStateType;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _lastStateType = StateToEnter;
        }

        protected override void OnUpdate(float deltaTime) {
            if (NpcAnimancer.MovementSpeed > 0.05f || math.abs(Npc.Controller.EstimatedAngularVelocity) > 0.05f) {
                ParentModel.SetCurrentState(NpcStateType.Movement);
                return;
            }
            
            if (_lastStateType != StateToEnter) {
                ParentModel.SetCurrentState(Type);
                return;
            }

            if (RemainingDuration <= BlendDuration) {
                ParentModel.SetCurrentState(Type, BlendDuration);
            }
        }
    }
}