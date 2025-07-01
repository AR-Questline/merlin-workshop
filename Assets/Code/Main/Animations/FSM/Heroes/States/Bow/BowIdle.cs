using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Bow {
    public partial class BowIdle : IdleBase<BowFSM> {
        protected override void AfterEnter(float previousStateNormalizedTime) {
            if (ParentModel.HeroBow != null) {
                ParentModel.HeroBow.OnBowIdle();
            }
            base.AfterEnter(previousStateNormalizedTime);
        }
    }
}