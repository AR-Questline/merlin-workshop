using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Legs {
    public partial class LegsJumpLoop : HeroAnimatorState<LegsFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Jumping;
        public override HeroStateType Type => HeroStateType.LegsJumpLoop;

        protected override void OnUpdate(float deltaTime) {
            if (ParentModel.HeroLanded) {
                ParentModel.SetCurrentState(HeroStateType.LegsJumpEnd, 0.1f);
            } else if (Hero.IsSwimming) {
                ParentModel.SetCurrentState(HeroStateType.Movement, 0.1f);
            }
        }
    }
}