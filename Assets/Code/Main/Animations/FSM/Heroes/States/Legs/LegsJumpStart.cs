using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Legs {
    public partial class LegsJumpStart : HeroAnimatorState<LegsFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Jumping;
        public override HeroStateType Type => HeroStateType.LegsJumpStart;

        protected override void OnUpdate(float deltaTime) {
            float timeElapsed = TimeElapsedNormalized;
            if (timeElapsed < 0.25f) {
                return;
            }
            
            if (ParentModel.HeroLanded) {
                ParentModel.SetCurrentState(HeroStateType.LegsJumpEnd, 0.05f);
                return;
            }

            if (timeElapsed > 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.LegsJumpLoop);
            }
        }
    }
}