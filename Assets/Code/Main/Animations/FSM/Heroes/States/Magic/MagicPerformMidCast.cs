using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.Main.Utility.UI;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    [UnityEngine.Scripting.Preserve]
    public partial class MagicPerformMidCast : HeroAnimatorState<MagicFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.MagicCastHeavy;
        public override HeroStateType Type => HeroStateType.MagicPerformMidCast;
        public override bool CanPerformNewAction => false;
        public override bool UsesActiveLayerMask => true;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            ParentModel.PlayAudioClip(ItemAudioType.CastRelease.RetrieveFrom(ParentModel.Item));
            Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData{effects = GameConstants.Get.magicPerformMidCastXboxVibrations, handsAffected = ParentModel.CastingHand});
        }

        protected override void OnUpdate(float deltaTime) {
            VibrationStrength vibrationStrength = TimeElapsedNormalized > 0.5f ? VibrationStrength.Low : VibrationStrength.Medium;
            RewiredHelper.VibrateHighFreq(vibrationStrength, VibrationDuration.Continuous);
            
            if (TimeElapsedNormalized >= 0.75f) {
                if (ParentModel.SpellAttackHeld) {
                    ParentModel.SetCurrentState(HeroStateType.MagicHeavyLoop);
                } else {
                    ParentModel.CancelCasting();
                }
            }
        }
    }
}