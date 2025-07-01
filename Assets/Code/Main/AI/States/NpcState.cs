using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.Utility;
using IState = Awaken.TG.Main.Utility.StateMachines.IState;
using StateMachine = Awaken.TG.Main.Utility.StateMachines.StateMachine;

namespace Awaken.TG.Main.AI.States {
    public interface INpcState : IState {
        NpcElement Npc { get; }
        public NpcAI AI { get; }
        public NpcData Data { get; }

        void OnDrawGizmos(AIDebug.Data data);
    }
    
    public abstract class NpcState<T> : State<T>, INpcState where T : StateMachine, INpcState {
        
        public NpcElement Npc => Parent.Npc;
        public NpcAI AI => Parent.AI;
        public NpcData Data => AI.Data;
        public NpcMovement Movement => Npc.Element<NpcMovement>();

        protected override void OnEnter() {
            NpcHistorian.NotifyStates(Npc, $"Npc Enter State: {GetType().Name}");
        }

        protected override void OnExit() {
            NpcHistorian.NotifyStates(Npc, $"Npc Exit State: {GetType().Name}");
        }

        public virtual void OnDrawGizmos(AIDebug.Data data) { }
    }

    public abstract class NpcStateMachine<T> : StateMachine<T>, INpcState where T : StateMachine, INpcState {
        public NpcElement Npc => Parent.Npc;
        public NpcAI AI => Parent.AI;
        public NpcData Data => AI.Data;

        public NpcMovement Movement => Npc.Element<NpcMovement>();

        protected override void OnEnter() {
            NpcHistorian.NotifyStates(Npc, $"Npc Enter SuperState: {GetType().Name}");
            base.OnEnter();
        }

        protected override void OnExit() {
            base.OnExit();
            NpcHistorian.NotifyStates(Npc, $"Npc Exit SuperState: {GetType().Name}");
        }

        protected override void OnStateChanged(IState previous, IState current) {
            base.OnStateChanged(previous, current);
            Npc.Trigger(NpcAI.Events.NpcStateChanged, new Change<IState>(previous, current));
            NpcState.TriggerChanged(Npc, previous, current);
        }

        public virtual void OnDrawGizmos(AIDebug.Data data) {
            (CurrentState as INpcState)?.OnDrawGizmos(data);
        }
    }
    
    public static class NpcState {
        public static event NpcStateChanged Changed;

        public static void TriggerChanged(NpcElement npc, IState previous, IState current) {
            Changed!(npc, previous, current);
        }
        
        public delegate void NpcStateChanged(NpcElement npc, IState previous, IState current);
    }
}