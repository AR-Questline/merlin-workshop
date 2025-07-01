using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Modifiers;
using Awaken.TG.Main.Character;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Melee {
    public abstract partial class LightAttackBase : MeleeAttackAnimatorState {
        bool _canPerformNextAttack;
        
        protected override bool CanPerform => _canPerformNextAttack;
        public override bool UsesActiveLayerMask => true;
        
        // === Initialization
        protected override void OnInitialize() {
            Hero.ListenTo(ICharacter.Events.OnAttackRecovery, () => _canPerformNextAttack = true, this);
            ParentModel.ListenTo(MeleeHitStop.Events.MeleeHitStopStarted, () => _canPerformNextAttack = true, this);
        }

        protected override bool BeforeEnter(out HeroStateType desiredState) {
            _canPerformNextAttack = false;
            return base.BeforeEnter(out desiredState);
        }
    }
}