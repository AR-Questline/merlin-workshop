using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Utility.UI;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    public partial class MagicFailedCast : HeroAnimatorState<MagicFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.MagicFailedCast;
        public override bool CanPerformNewAction => TimeElapsedNormalized >= 0.75f;
        public override bool UsesActiveLayerMask => true;

        protected override bool BeforeEnter(out HeroStateType desiredState) {
            ParentModel.EndSlowModifier();
            return base.BeforeEnter(out desiredState);
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            ParentModel.ResetAttackProlong();
            ParentModel.ResetBlockProlong();
            
            RewiredHelper.VibrateLowFreq(VibrationStrength.Medium, VibrationDuration.Short);
            RewiredHelper.VibrateHighFreq(VibrationStrength.Low, VibrationDuration.Short);
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
            }
        }
        
        protected override void OnFailedFindNode() {
            if (!ParentModel.IsLayerActive) {
                return;
            }
            
            if (ParentModel.CurrentAnimatorState != this) {
                return;
            }
            
            ParentModel.SetCurrentState(HeroStateType.Idle, 0f);
        }
    }
}