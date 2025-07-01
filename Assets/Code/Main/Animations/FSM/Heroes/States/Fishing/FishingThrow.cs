using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Fishing {
    public partial class FishingThrow : HeroAnimatorState<FishingFSM> {
        static float ThrowTime => Main.Heroes.Hero.TppActive ? 0.62f : 0.25f;

        bool _throwEventNotTriggered;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Interaction;
        public override HeroStateType Type => HeroStateType.FishingThrow;
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _throwEventNotTriggered = true;
            Hero.Trigger(FishingFSM.Events.StartThrow, Hero);
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= ThrowTime && _throwEventNotTriggered) {
                _throwEventNotTriggered = false;
                Hero.Trigger(FishingFSM.Events.Throw, Hero);
            }
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.FishingIdle);
            }
        }
    }
}