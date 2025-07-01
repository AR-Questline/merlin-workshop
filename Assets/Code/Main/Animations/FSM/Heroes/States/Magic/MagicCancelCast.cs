using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    public partial class MagicCancelCast : HeroAnimatorState<MagicFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.MagicCancelCast;
        public override bool CanPerformNewAction => TimeElapsedNormalized <= 0.01f;
        public override float EntryTransitionDuration => ParentModel.WasCanceledWhenInMagicLoop ? 0.25f : 0f;
        protected override float OffsetNormalizedTime(float previousNormalizedTime) => ParentModel.WasCanceledWhenInMagicLoop ? 1 : 1 - previousNormalizedTime;
        public override bool UsesActiveLayerMask => true;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            ParentModel.EndSlowModifier();
            Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData {effects = GameConstants.Get.magicCancelCastXboxVibrations, handsAffected = ParentModel.CastingHand});
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized <= 0f) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
            }
        }

        protected override void OnExit(bool restarted) {
            ParentModel.ResetProlong();
        }
    }
}