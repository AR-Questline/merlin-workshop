using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Fishing {
    public partial class FishingFightStart : HeroAnimatorState<FishingFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Interaction;
        public override HeroStateType Type => HeroStateType.FishingFightStart;

        bool _catchEventNotTriggered;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _catchEventNotTriggered = true;
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.6 && _catchEventNotTriggered) {
                _catchEventNotTriggered = false;
                Hero.Trigger(FishingFSM.Events.StartFight, Hero);
            }
        }
    }
}