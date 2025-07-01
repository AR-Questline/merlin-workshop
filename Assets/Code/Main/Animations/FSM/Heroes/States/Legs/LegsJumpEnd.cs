using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Legs {
    public partial class LegsJumpEnd : HeroAnimatorState<LegsFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Jumping;
        public override HeroStateType Type => HeroStateType.LegsJumpEnd;
        public override HeroStateType StateToEnter =>
            ParentModel.VerticalVelocityOnLand switch {
                > -12f => HeroStateType.LegsJumpEnd,
                > -20f => HeroStateType.LegsJumpEndMedium,
                _ => HeroStateType.LegsJumpEndHigh
            };

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.Idle, 0.1f);
            }
        }
    }
}