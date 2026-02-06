using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Shared {
    public class HeadBobbingIdle : IdleBase<IdleFSM> {
        protected override bool BeforeEnter(out HeroStateType desiredState) {
            desiredState = HeroStateType.Invalid;
            return true;
        }
    }
}