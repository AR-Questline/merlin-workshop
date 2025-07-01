using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    public partial class MagicHeavyStart : HeroAnimatorState<MagicFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.MagicCastHeavy;
        public override HeroStateType Type => HeroStateType.MagicHeavyStart;
        public override bool CanPerformNewAction => TimeElapsedNormalized >= 0.75f;
        public override bool UsesActiveLayerMask => true;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            ParentModel.CurrentChargeSteps = 0;
            Hero.VHeroController?.CastingBegun(ParentModel.CastingHand);
            ParentModel.PlayAudioClip(ItemAudioType.CastBegun.RetrieveFrom(ParentModel.Item));
            ParentModel.BeginSlowModifier();
            RewiredHelper.VibrateLowFreq(VibrationStrength.Low, VibrationDuration.Short);
        }

        protected override void OnUpdate(float deltaTime) {
            VibrationStrength vibrationStrength = TimeElapsedNormalized > 0.5f ? VibrationStrength.Low : VibrationStrength.Medium;
            Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData{effects = GameConstants.Get.magicHeavyXboxVibrations, handsAffected = ParentModel.CastingHand});
            
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(ParentModel.CanBeCharged
                    ? HeroStateType.MagicHeavyChargeLoop
                    : HeroStateType.MagicHeavyLoop);
            }
        }
    }
}