using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.States.TPP;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Shared {
    public abstract partial class IdleBase<T> : HeroAnimatorState<T>, ISynchronizedAnimatorState where T : HeroAnimatorSubstateMachine {
        HeroStateType _lastIdleState;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.Idle;
        public override HeroStateType StateToEnter => UseAlternateState ? HeroStateType.IdleAlternate : HeroStateType.Idle;
        public override bool CanReEnter => true;
        protected override bool HeadBobbingDependent => true;
        protected override float OffsetNormalizedTime(float previousNormalizedTime) => ParentModel.SynchronizedStateOffsetNormalizedTime();
        protected virtual bool ExitToMovementCondition => !Hero.IsInHitStop && Hero.Grounded;

        protected override bool BeforeEnter(out HeroStateType desiredState) {
            if (!Hero.IsWeaponEquipped) {
                desiredState = HeroStateType.Empty;
                return false;
            }
            return base.BeforeEnter(out desiredState);
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _lastIdleState = StateToEnter;
            base.AfterEnter(previousStateNormalizedTime);
        }

        protected override void OnUpdate(float deltaTime) {
            if (HeroAnimancer.MovementSpeed > 0.05f && ExitToMovementCondition) {
                ParentModel.SetCurrentState(HeroStateType.Movement);
                return;
            }
            
            if (_lastIdleState != StateToEnter) {
                ParentModel.SetCurrentState(Type);
            }
        }
    }
}