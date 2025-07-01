using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides {
    public partial class HeroPetSharg : HeroAnimatorState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.PetSharg;
        bool _eventInvoked;

        public new static class Events {
            public static readonly Event<Hero, bool> PetShargEnded = new(nameof(PetShargEnded));
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _eventInvoked = false;
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.99f) {
                ParentModel.SetCurrentState(Hero.TppActive ? HeroStateType.Idle : HeroStateType.None, 0.01f);
            }
        }

        protected override void OnExit(bool restarted) {
            if (_eventInvoked) {
                return;
            }
            
            Hero.Current.Trigger(Events.PetShargEnded, true);
            _eventInvoked = true;
        }
    }
}