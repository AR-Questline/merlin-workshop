using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Fishing {
    public partial class FishingPullOut : HeroAnimatorState<FishingFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Interaction;
        public override HeroStateType Type => HeroStateType.FishingPullOut;
        bool _pullOutEventNotTriggered;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _pullOutEventNotTriggered = true;
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.4 && _pullOutEventNotTriggered) {
                _pullOutEventNotTriggered = false;
                Hero.Trigger(FishingFSM.Events.PullOut, Hero);
            }
            
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.FishingInspect);
            }
        }
    }
}