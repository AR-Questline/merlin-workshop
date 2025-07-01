using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Fishing {
    public partial class FishingBiteLoop : HeroAnimatorState<FishingFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Interaction;
        public override HeroStateType Type => HeroStateType.FishingBiteLoop;
    }
}