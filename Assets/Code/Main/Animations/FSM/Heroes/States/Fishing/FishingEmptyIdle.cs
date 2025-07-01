using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Fishing {
    public partial class FishingEmptyIdle : HeroAnimatorState<FishingFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Interaction;
        public override HeroStateType Type => HeroStateType.Idle;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            Hero.Trigger(FishingFSM.Events.Abort, Hero);
            World.Any<HeroInteractionUI>()?.TriggerChange();
        }
    }
}