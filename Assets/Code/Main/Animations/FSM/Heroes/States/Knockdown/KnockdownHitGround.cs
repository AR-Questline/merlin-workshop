using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Knockdown {
    public partial class KnockdownHitGround : HeroAnimatorState {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.KnockdownHitGround;
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized > 0.75f) {
                ParentModel.SetCurrentState(Hero.Grounded ? HeroStateType.KnockdownGroundLoop : HeroStateType.KnockdownAirLoop);
            }
        }
    }
}