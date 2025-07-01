using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.BackStab {
    public partial class BackStabExit : HeroAnimatorState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.BackStabExit;
        public override bool CanPerformNewAction => TimeElapsedNormalized > 0.75f;
        public override bool UsesActiveLayerMask => true;
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.75) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
            }
        }
    }
}