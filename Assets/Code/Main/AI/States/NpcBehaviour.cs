#if DEBUG && !NPC_LOGIC_DEBUGGING
#define NPC_LOGIC_DEBUGGING
#endif

using System.Collections.Generic;
using Awaken.TG.Main.AI.Debugging;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds.CullingGroupSystem;
using Awaken.TG.Main.Grounds.CullingGroupSystem.CullingGroups;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.Utility;

namespace Awaken.TG.Main.AI.States {
    public class NpcBehaviour : StateMachine, INpcState {
        bool? _DEBUG_OverrideShouldWorking;

        StateAINotWorking NotWorking { get; }
        StateAIWorking Working { get; }
        StateAIPaused Paused { get; }
        
        public bool? DEBUG_OverrideShouldWorking {
            get => _DEBUG_OverrideShouldWorking;
            set {
                if (_DEBUG_OverrideShouldWorking == value) {
                    return;
                }
                _DEBUG_OverrideShouldWorking = value;
                Npc.ParentModel.Trigger(ICullingSystemRegistreeModel.Events.DistanceBandChanged, Npc.ParentModel.GetCurrentBandSafe(LocationCullingGroup.LastBand));
            }
        }

        public NpcElement Npc => AI.ParentModel;
        public NpcAI AI { get; }
        public NpcData Data => AI.Data;
        public NpcDangerTracker DangerTracker => Working.Perception.DangerTracker;

        public NpcBehaviour(NpcAI npcAI) {
            AI = npcAI;
            NotWorking = new StateAINotWorking();
            Working = new StateAIWorking();
            Paused = new StateAIPaused();
        }
        
        protected override IEnumerable<IState> States() {
            yield return NotWorking;
            yield return Working;
            if (!Npc.IsHeroSummon) {
                yield return Paused;
            }
        }

        protected override IState InitialState => LocationCullingGroup.InActiveLogicBands(Npc.ParentModel.GetCurrentBandSafe(LocationCullingGroup.LastBand)) ? Working : NotWorking;

        protected override IEnumerable<StateTransition> Transitions() {
            yield return new StateTransition(NotWorking, Working, ListenTransition.New(Npc.ParentModel, ICullingSystemRegistreeModel.Events.DistanceBandChanged, ShouldWorkInBand));
            yield return new StateTransition(Working, NotWorking, ListenTransition.New(Npc.ParentModel, ICullingSystemRegistreeModel.Events.DistanceBandChanged, ShouldNotWorkInBand));
            if (!Npc.IsHeroSummon) {
                yield return new StateTransition(Working, Paused, ListenTransition.New(Npc.ParentModel, ICullingSystemRegistreeModel.Events.DistanceBandPauseChanged, ShouldBePaused));
                yield return new StateTransition(Paused, Working, ListenTransition.New(Npc.ParentModel, ICullingSystemRegistreeModel.Events.DistanceBandPauseChanged, ShouldNotBePaused));
            }
        }

        protected override void OnStateChanged(IState previous, IState current) {
            base.OnStateChanged(previous, current);
            Npc.Trigger(NpcAI.Events.NpcStateChanged, new Change<IState>(previous, current));
        }

        bool ShouldWorkInBand(int bond) {
#if NPC_LOGIC_DEBUGGING
            if (DEBUG_OverrideShouldWorking.HasValue) {
                return DEBUG_OverrideShouldWorking.Value;
            }
#endif
            return LocationCullingGroup.InActiveLogicBands(bond);
        }
        
        bool ShouldBePaused(bool isPaused) {
            return isPaused;
        }
        bool ShouldNotBePaused(bool isPaused) {
            return !isPaused;
        }

        bool ShouldNotWorkInBand(int bond) {
#if NPC_LOGIC_DEBUGGING
            if (DEBUG_OverrideShouldWorking.HasValue) {
                return !DEBUG_OverrideShouldWorking.Value;
            }
#endif
            return !LocationCullingGroup.InActiveLogicBands(bond);
        }

        public void OnDrawGizmos(AIDebug.Data data) {
            (CurrentState as INpcState)?.OnDrawGizmos(data);
        }
    }
}