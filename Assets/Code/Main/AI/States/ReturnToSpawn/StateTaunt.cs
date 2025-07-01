using Awaken.TG.Main.AI.Combat.Behaviours.BaseBehaviours;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;

namespace Awaken.TG.Main.AI.States.ReturnToSpawn {
    public abstract class StateTaunt : NpcState<StateReturn> {
        NoMove _noMove;
        TauntStateType _tauntStateType;

        public bool TauntEnded => _tauntStateType == TauntStateType.Taunted;
        protected abstract NpcStateType StateToEnter { get; }
        
        public override void Init() {
            _noMove = new NoMove();
        }
        
        protected override void OnEnter() {
            base.OnEnter();
            if (!Npc.EnemyBaseClass.HasElement<TauntBehaviour>()) {
                _tauntStateType = TauntStateType.Taunted;
                return;
            }
            _tauntStateType = TauntStateType.Taunting;
            Movement.ChangeMainState(_noMove);
            Npc.SetAnimatorState(NpcFSMType.GeneralFSM, StateToEnter);
        }

        public override void Update(float deltaTime) {
            if (TauntEnded) {
                return;
            }
            
            if (Npc.Element<NpcGeneralFSM>().CurrentAnimatorState.Type != StateToEnter) {
                _tauntStateType = TauntStateType.Taunted;
            }
        }

        protected override void OnExit() {
            base.OnExit();
            _tauntStateType = TauntStateType.None;
        }

        enum TauntStateType : byte {
            None = 0,
            Taunting = 1,
            Taunted = 2,
        }
    }
}