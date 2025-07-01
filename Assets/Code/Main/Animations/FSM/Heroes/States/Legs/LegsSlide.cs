using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Legs {
    public partial class LegsSlide : HeroAnimatorState<LegsFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Sliding;
        public override HeroStateType Type => HeroStateType.LegsSlide;

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
            }
        }
    }
}