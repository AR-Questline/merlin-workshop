using Awaken.TG.Main.AI.Combat.Behaviours.Abstracts;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.Heroes.Statuses.Duration {
    public partial class UntilEndOfCombatBehaviour : NonEditableDuration<IWithDuration>, IDuration {
        public sealed override bool IsNotSaved => true;

        // === Fields
        readonly EnemyBehaviourBase _behaviour;
        
        // === Properties
        public override bool Elapsed => false;
        public override string DisplayText => string.Empty;
        
        // === Constructor
        public UntilEndOfCombatBehaviour(EnemyBehaviourBase behaviour) {
            _behaviour = behaviour;
        }
        
        // === Initialization
        protected override void OnInitialize() {
            _behaviour.ListenTo(EnemyBehaviourBase.Events.BehaviourExited, Discard, this);
            _behaviour.ListenTo(Events.BeforeDiscarded, Discard, this);
        }
    }
}