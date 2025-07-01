using Animancer;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Utility.Animations.ARTransitions;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcTurnMovement : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcTurnMovement;

        const float ExitDurationFromTurnMovement = 0.2f;
        
        public override NpcStateType Type => NpcStateType.TurnMovement;
        public override bool CanOverrideDestination => false;
        public override bool ResetMovementSpeed => true;

        protected override NpcStateType StateToEnter => GetCurrentMovementMovementState switch {
            NpcMovementState.Combat => NpcStateType.CombatMovement,
            NpcMovementState.Alert => NpcStateType.AlertMovement,
            _ => NpcStateType.Movement,
        };
        
        protected virtual NpcStateType StateToReturnTo => NpcStateType.Movement;
        
        protected override void OnNodeLoaded(ITransition node, float? overrideCrossFadeTime) {
            var overridenNode = GetTurningOverrideForTransition(node) ?? node;
            base.OnNodeLoaded(overridenNode, overrideCrossFadeTime);
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= ExitDurationFromTurnMovement) {
                ParentModel.SetCurrentState(StateToReturnTo);
            }
        }
        
        ITransition GetTurningOverrideForTransition(ITransition transition) {
            return transition switch {
                MixerTransition2DAsset mixerAsset => GetTurningOverrideForTransition(mixerAsset.Transition),
                ARMixerTransition arMixerTransition => arMixerTransition.TurningOverrides.GetOverrideFor(Npc),
                _ => null
            };
        }
    }
}