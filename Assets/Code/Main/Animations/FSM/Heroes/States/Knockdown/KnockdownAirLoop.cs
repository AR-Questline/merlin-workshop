using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Knockdown {
    public partial class KnockdownAirLoop : HeroAnimatorState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.KnockdownAirLoop;

        protected override void OnUpdate(float deltaTime) {
            if (Hero.Grounded) {
                ParentModel.SetCurrentState(HeroStateType.KnockdownHitGround);
            }
        }
    }
}