using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.CameraShakes.Dash {
    public abstract partial class DashBaseState : HeroAnimatorState {
        readonly HeroStateType _exitState;
        
        public override bool CanReEnter => true;
        
        protected DashBaseState(HeroStateType exitState) {
            _exitState = exitState;
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.9f) {
                ParentModel.SetCurrentState(_exitState, 0.1f);
            }
        }
    }
}