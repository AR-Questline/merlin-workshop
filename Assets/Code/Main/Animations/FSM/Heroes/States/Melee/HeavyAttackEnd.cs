using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.Modifiers;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Utility.Animations.HitStops;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Melee {
    public partial class HeavyAttackEnd : MeleeAttackAnimatorState {
        // === Fields
        bool _canPerformNextAttack;
        
        // === Properties
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.HeavyAttack;
        public override HeroStateType Type => HeroStateType.HeavyAttackEnd;
        public override HeroStateType StateToEnter => ParentModel.HeavyAttackIndex <= 1
            ? HeroStateType.HeavyAttackEnd
            : HeroStateType.HeavyAttackEndAlternate;
        public override float EntryTransitionDuration => 0.1f;
        public override bool IsUsingMainHand => true;
        protected override bool CanPerform => _canPerformNextAttack;
        protected override bool IsHeavy => true;
        protected override HitStopData HitStopData => ParentModel is OneHandedFSM ? HitStopsAsset.heavyAttack1HData : HitStopsAsset.heavyAttack2HData;
        protected virtual float HeavyAttackCost => ParentModel.HeavyAttackCost;
        public override bool UsesActiveLayerMask => true;
        
        protected override void OnInitialize() {
            Hero.ListenTo(ICharacter.Events.OnAttackRecovery, () => _canPerformNextAttack = true, this);
            ParentModel.ListenTo(MeleeHitStop.Events.MeleeHitStopStarted, () => _canPerformNextAttack = true, this);
        }
        
        protected override bool BeforeEnter(out HeroStateType desiredState) {
            _canPerformNextAttack = false;
            return base.BeforeEnter(out desiredState);
        }
        
        protected override void OnAfterEnter(float previousStateNormalizedTime) {
            Stamina.DecreaseBy(HeavyAttackCost);
        }

        protected override void OnUpdate(float deltaTime) {
            base.OnUpdate(deltaTime);
            
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
            }
        }

        protected override void OnAfterExit() {
            ParentModel.ResetAttackProlong();
        }
    }
}