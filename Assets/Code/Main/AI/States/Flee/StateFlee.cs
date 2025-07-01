using System.Collections.Generic;
using Awaken.TG.Main.AI.Combat.Utils;
using Awaken.TG.Main.Fights.Factions.Crimes;
using Awaken.TG.Main.Heroes.Thievery;
using Awaken.TG.Main.Utility.StateMachines;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.AI.States.Flee {
    public class StateFlee : NpcStateMachine<StateAIWorking> {
        bool _isAlwaysFleeing;
        bool _hasBeenPickpocketed;
        
        public EmptyState<StateFlee> Root { get; } = new();
        public StateFear Fear { get; } = new();
        public StateFleeInAdditive FleeInAdditive { get; } = new();
        public StateFleeInOverworld FleeInOverworld { get; } = new();
        
        protected override IEnumerable<IState> States() {
            yield return Root;
            yield return Fear;
            yield return FleeInAdditive;
            yield return FleeInOverworld;
        }

        protected override IEnumerable<StateTransition> Transitions() {
            yield return new(Root, Fear, new PollTransition(Root2Fear));
            yield return new(Fear, FleeInAdditive, new PollTransition(Fear2FleeInAdditive));
            yield return new(Fear, FleeInOverworld, new PollTransition(Fear2FleeInOverworld));
            
            yield return new(Root, FleeInAdditive, new PollTransition(Root2FleeInAdditive));
            yield return new(Root, FleeInOverworld, new PollTransition(Root2FleeInOverworld));
        }

        protected override void OnEnter() {
            _isAlwaysFleeing = CrimeReactionUtils.IsAlwaysFleeing(Npc);
            
            var npcCrimeReactions = Npc.Element<NpcCrimeReactions>();
            _hasBeenPickpocketed = npcCrimeReactions.HasBeenPickpocketed;
            
            base.OnEnter();
            AI.InFlee = true;
        }

        protected override void OnExit() {
            base.OnExit();
            AI.InFlee = false;
        }

        bool Root2Fear() => !UseInstantFlee();
        bool Fear2FleeInAdditive() => (Parent.AreFearfulsInDanger || _hasBeenPickpocketed) && InAdditive();
        bool Fear2FleeInOverworld() => (Parent.AreFearfulsInDanger || _hasBeenPickpocketed) && InOverworld();
        bool Root2FleeInAdditive() => UseInstantFlee() && InAdditive();
        bool Root2FleeInOverworld() => UseInstantFlee() && InOverworld();
        
        bool UseInstantFlee() => _isAlwaysFleeing || Parent.AreFearfulsInDanger || _hasBeenPickpocketed;
        
        bool InAdditive() => World.Services.Get<SceneService>().IsAdditiveScene;
        bool InOverworld() => !World.Services.Get<SceneService>().IsAdditiveScene;
    }
}