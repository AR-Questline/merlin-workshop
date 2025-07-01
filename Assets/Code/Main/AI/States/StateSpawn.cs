using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.AI.States {
    public class StateSpawn : NpcState<StateAIWorking> {
        bool _spawningEventTriggered;
        NpcGeneralFSM _animatorSubstateMachine;
        bool _hasBeenInSpawnBefore;
        public bool CanEnter => Npc.StartInSpawn && !_hasBeenInSpawnBefore;
        public bool CanLeave { get; private set; }
        
        protected override void OnEnter() {
            base.OnEnter();
            AI.InSpawn = true;
            _hasBeenInSpawnBefore = true;
            CanLeave = false;
            NpcElement npc = Npc;
            npc.ListenToLimited(NpcElement.Events.AnimatorEnteredSpawnState, TryTriggerSpawningEvent, npc);
            npc.SetAnimatorState(NpcFSMType.GeneralFSM, NpcStateType.Spawn);
            npc.LastIdlePosition = npc.LastOutOfCombatPosition = npc.Coords;
            _animatorSubstateMachine = npc.Element<NpcGeneralFSM>();
        }
        
        public override void Update(float deltaTime) {
            if (_animatorSubstateMachine.CurrentAnimatorState.Type != NpcStateType.Spawn) {
                CanLeave = true;
                TryTriggerSpawningEvent();
            }
        }
        
        void TryTriggerSpawningEvent() {
            if (!_spawningEventTriggered) {
                Npc.Trigger(NpcElement.Events.NpcSpawning, Npc);
                _spawningEventTriggered = true;
            }
        }
        
        protected override void OnExit() {
            base.OnExit();
            AI.InSpawn = false;
            _animatorSubstateMachine = null;
        }
    }
}