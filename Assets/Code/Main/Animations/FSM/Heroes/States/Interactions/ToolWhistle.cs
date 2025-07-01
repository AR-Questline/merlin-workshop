using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Interactions {
    public partial class ToolWhistle : HeroAnimatorState<ToolInteractionFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.Whistle;

        bool _mountCalled;

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.4f && !_mountCalled) {
                _mountCalled = true;
                Hero.CallMount();
            }

            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.None);
            }
        }
        
        protected override void OnExit(bool restarted) {
            _mountCalled = false;
        }
    }
}
