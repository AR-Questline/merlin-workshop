using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public class NpcPetVariantInteractionState : NpcAnimatorState<NpcGeneralFSM> {
        public sealed override bool IsNotSaved => true;

        bool _canBeExited;
        
        bool IsTransition => Type is NpcStateType.PetVariantTransition or NpcStateType.PetVariantTransitionLarge;
        public override bool CanBeExited => _canBeExited || !IsTransition;
        public override NpcStateType Type { get; }
        public override bool ResetMovementSpeed => true;
        public override bool CanReEnter => true;
        
        public NpcPetVariantInteractionState(NpcStateType type) {
            Type = type;
        }
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _canBeExited = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                _canBeExited = true;
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
    }
}