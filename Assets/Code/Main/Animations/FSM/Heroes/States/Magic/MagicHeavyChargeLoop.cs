using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Utility.Audio;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Magic {
    public partial class MagicHeavyChargeLoop : HeroAnimatorState<MagicFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.MagicCastHeavy;
        public override HeroStateType Type => HeroStateType.MagicHeavyChargeLoop;
        public override bool UsesActiveLayerMask => true;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            CurrentState.Speed = Hero.CharacterStats.SpellChargeSpeed;
            
            ParentModel.PlayAudioClip(ItemAudioType.CastCharging.RetrieveFrom(ParentModel.Item));
        }

        protected override void OnUpdate(float deltaTime) {
            float timeElapsedNormalized = TimeElapsedNormalized;
            ParentModel.Skill?.ChargeLevelIncrease(timeElapsedNormalized);
            
            if (timeElapsedNormalized > 0.9f) {
                ParentModel.SetCurrentState(HeroStateType.MagicHeavyChargeIncrease, 0.1f);
            }
        }
    }
}