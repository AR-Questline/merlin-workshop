using System.Collections.Generic;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.AI.States.CrimeReactions {
    public class StateCrimeReaction : NpcStateMachine<StateAIWorking> {
        // == Substates
        StateInvestigation Investigation { get; } = new();
        public StatePlayerSuspicious PlayerSuspicious { get; } = new();
        StateSearchForCriminal Search { get; } = new();
        
        protected override IEnumerable<IState> States() {
            yield return Investigation;
            yield return PlayerSuspicious;
            yield return Search;
        }

        protected override IEnumerable<StateTransition> Transitions() {
            yield return new StateTransition(Investigation, Search, new PollTransition(InvestigationToSearchCrime));
        }

        protected override void OnInit() {
        }

        protected override void OnEnter() {
            AI.InCrimeReaction = true;
            base.OnEnter();
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);
        }

        protected override void OnExit() {
            AI.InCrimeReaction = false;
            base.OnExit();
        }

        bool InvestigationToSearchCrime() => Investigation.DestinationReached;
    }
}