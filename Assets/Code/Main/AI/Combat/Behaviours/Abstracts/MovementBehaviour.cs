using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Utility.Animations;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    public abstract partial class MovementBehaviour<T> : MovementBehaviour where T : EnemyBaseClass {
        protected new T ParentModel => base.ParentModel as T;
    }

    public abstract partial class MovementBehaviour : EnemyBehaviourBase {
        public override bool CanBeInterrupted => true;
        public override bool CanBeAggressive => true;
        public override bool IsPeaceful => true;
        public override bool AllowStaminaRegen => true;
        public override bool RequiresCombatSlot => false;
    }
}