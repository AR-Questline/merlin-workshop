using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Overrides {
    public partial class HeroPraySuccess : HeroAnimatorState<HeroOverridesFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.HeroPraySuccess;
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.99f) {
                ParentModel.SetCurrentState(HeroStateType.None);
            }
        }
    }
}
