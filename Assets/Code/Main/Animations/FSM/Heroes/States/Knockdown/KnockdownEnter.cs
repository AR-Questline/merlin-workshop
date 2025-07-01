using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Knockdown {
    public partial class KnockdownEnter : HeroAnimatorState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.KnockdownEnter;

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.9f) {
                ParentModel.SetCurrentState(Hero.Grounded ? HeroStateType.KnockdownHitGround : HeroStateType.KnockdownAirLoop, 0.1f);
            }
        }
    }
}