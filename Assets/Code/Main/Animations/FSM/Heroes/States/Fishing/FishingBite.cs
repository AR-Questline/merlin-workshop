using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Fishing {
    public partial class FishingBite : HeroAnimatorState<FishingFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Interaction;
        public override HeroStateType Type => HeroStateType.FishingBite;
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.75) {
                ParentModel.SetCurrentState(HeroStateType.FishingBiteLoop);
            }
        }
    }
}