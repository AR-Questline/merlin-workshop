using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Knockdown {
    public partial class KnockdownGroundLoop : HeroAnimatorState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.KnockdownGroundLoop;

        [UnityEngine.Scripting.Preserve]
        public void ExitGroundLoop() {
            ParentModel.SetCurrentState(HeroStateType.KnockdownEnd);
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            Hero.HeroKnockdown?.KnockdownGroundLoopStarted();
        }
    }
}