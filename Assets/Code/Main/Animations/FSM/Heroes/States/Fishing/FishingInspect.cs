using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Fishing {
    public partial class FishingInspect : HeroAnimatorState<FishingFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Interaction;
        public override HeroStateType Type => HeroStateType.FishingInspect;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            Hero.Trigger(FishingFSM.Events.Inspect, Hero);
        }
    }
}