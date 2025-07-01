using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    public partial class EnemyBehaviourCooldown : DurationProxy<EnemyBehaviourBase> {
        public sealed override bool IsNotSaved => true;

        public override IModel TimeModel => ParentModel.ParentModel.ParentModel;

        EnemyBehaviourCooldown(IDuration duration) : base(duration) { }

        public static void Cooldown(EnemyBehaviourBase behaviour, IDuration cooldown) {
            if (behaviour == null) {
                return;
            }
            EnemyBehaviourCooldown behaviourCooldown = behaviour.TryGetElement<EnemyBehaviourCooldown>();
            if (behaviourCooldown != null) {
                behaviourCooldown.Prolong(cooldown);
            } else {
                behaviour.AddElement(new EnemyBehaviourCooldown(cooldown));
            }
        }
    }
}