using Awaken.TG.Main.AI.Combat.Attachments;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.AI.Combat.Behaviours.Abstracts {
    public interface IBehaviourBase : IElement<EnemyBaseClass> {
        int Weight { get; }
        int Priority { get; }
        bool CanMove { get; }
        bool CanBeInvoked { get; }
        bool CanBeInterrupted { get; }
        bool AllowStaminaRegen { get; }
        bool RequiresCombatSlot { get; }
        /// <summary>
        /// CanBeAggressive answers for question "Can I be aggressive while doing this action?".
        /// CanBeAggressive is set to false in behaviours like Stumble/Stagger -> That is I'm performing action that don't allow me to fight.
        /// </summary>
        bool CanBeAggressive { get; }
        bool IsPeaceful { get; }
        bool CanBlockDamage { get; }
        bool Start();
        void NotInCombatUpdate(float deltaTime) { Update(deltaTime); }
        void Update(float deltaTime);
        void Stop();
        void Interrupt();
        void TriggerAnimationEvent(ARAnimationEvent animationEvent);
    }
}