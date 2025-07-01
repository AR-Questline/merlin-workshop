using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides {
    public partial class HeroCustomInteractionAnimation : HeroAnimatorState<HeroOverridesFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.HeroCustomInteractionAnimation;
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.None);
            }
        }
    }
}