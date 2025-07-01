using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.BackStab {
    public partial class BackStabEnter : HeroAnimatorState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.BackStab;
        public override HeroStateType Type => HeroStateType.BackStabEnter;
        public override bool UsesActiveLayerMask => true;
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.BackStabLoop);
            }
        }
    }
}