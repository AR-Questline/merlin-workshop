using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Fishing {
    public partial class FishingTakeFish : HeroAnimatorState<FishingFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Interaction;
        public override HeroStateType Type => HeroStateType.FishingTakeFish;
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.95f) {
                ParentModel.SetCurrentState(HeroStateType.Idle, 0f);
            }
        }
    }
}