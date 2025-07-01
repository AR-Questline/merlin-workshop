using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.TPP {
    public partial class LegsIdle : IdleBase<LegsFSM> {
        public override HeroStateType StateToEnter => Hero.IsSwimming
            ? HeroStateType.LegsSwimmingIdle
            : ParentModel.ShouldCrouch
                ? HeroStateType.CrouchedIdle
                : base.StateToEnter;

        protected override float OffsetNormalizedTime(float previousNormalizedTime) => ParentModel.SynchronizedStateOffsetNormalizedTime();
        protected override bool ExitToMovementCondition => Hero.IsSwimming || base.ExitToMovementCondition;
        
        protected override bool BeforeEnter(out HeroStateType desiredState) {
            desiredState = HeroStateType.Invalid;
            return true;
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (Hero.Mounted) {
                ParentModel.SetCurrentState(HeroStateType.Movement);
                return;
            }
            
            base.OnUpdate(deltaTime);
        }
    }
}