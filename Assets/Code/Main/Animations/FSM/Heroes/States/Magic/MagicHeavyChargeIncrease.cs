using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    public partial class MagicHeavyChargeIncrease : HeroAnimatorState<MagicFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.MagicCastHeavy;
        public override HeroStateType Type => HeroStateType.MagicHeavyChargeIncrease;
        public override bool UsesActiveLayerMask => true;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            int currentCharge = ++ParentModel.CurrentChargeSteps;
            ParentModel.Skill?.ChargeStepIncrease(currentCharge);
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.9f) {
                if (ParentModel.CurrentChargeSteps >= ParentModel.MaxChargeSteps) {
                    ParentModel.SetCurrentState(HeroStateType.MagicHeavyLoop, 0.1f);
                } else {
                    ParentModel.SetCurrentState(HeroStateType.MagicHeavyChargeLoop, 0.1f);
                }
            }
        }
    }
}