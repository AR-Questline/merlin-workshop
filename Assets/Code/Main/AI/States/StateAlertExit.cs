using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;

namespace Awaken.TG.Main.AI.States {
    public class StateAlertExit : NpcState<StateAlert> {
        readonly NoMove _noMove = new();

        NpcAnimatorSubstateMachine _substateMachine;
        
        protected override void OnEnter() {
            base.OnEnter();
            Parent.CanExitToIdle = false;
            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.AlertExit);
            _substateMachine = Npc.GetAnimatorSubstateMachine(NpcFSMType.GeneralFSM);
        }

        public override void Update(float deltaTime) {
            UpdateMovementMainState();
            if (_substateMachine.CurrentAnimatorState.Type != NpcStateType.AlertExit) {
                Parent.CanExitToIdle = true;
            }
        }
        
        void UpdateMovementMainState() {
            Movement.ChangeMainState(_noMove);
        }
    }
}