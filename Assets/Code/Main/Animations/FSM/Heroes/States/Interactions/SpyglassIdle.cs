using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Interactions {
    public partial class SpyglassIdle : HeroAnimatorState<SpyglassFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.Idle;
        
        protected override void OnUpdate(float deltaTime) {
            if (HeroAnimancer.MovementSpeed > 0.05f && !Hero.IsInHitStop) {
                ParentModel.SetCurrentState(HeroStateType.Movement);
            }
        }
    }
}