using Awaken.TG.Graphics.Animations;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Bow {
    public partial class BowPull : HeroAnimatorState<BowFSM>, IStateWithModifierAttackSpeed {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.BowDraw;
        public override HeroStateType Type => HeroStateType.BowPull;
        public float AttackSpeed => ParentModel.GetAttackSpeed(false); 

        protected override void AfterEnter(float previousStateNormalizedTime) {
            ParentModel.ResetAttackProlong();
            if (Hero.MainHandWeapon != null) {
                Hero.MainHandWeapon.PlayAudioClip(ItemAudioType.DragBow);
            }
            ParentModel.BeginSlowModifier();
            Hero.Trigger(ICharacter.Events.OnBowDrawStart, Hero);
            AnimatorUtils.StartProcessingAnimationSpeed(ParentModel.HeroAnimancer, ParentModel.AnimancerLayer, ParentModel.LayerType, StateToEnter, false, WeaponRestriction.None);

            if (ParentModel.HeroBow != null) {
                ParentModel.HeroBow.OnPullBow();
            }
        }

        protected override void OnUpdate(float deltaTime) {
            if (CurrentState != null) {
                CurrentState.Speed = AttackSpeed;
            }
            
            float staminaTick = ParentModel.StatsItemStats ? ParentModel.StatsItemStats.DrawBowCostPerTick.ModifiedValue * Hero.HeroStats.ItemStaminaCostMultiplier : 0;
            
            if (StaminaUsedUpEffect.TryDecreaseContinuously(staminaTick, deltaTime)) {
                Hero.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData {effects = GameConstants.Get.bowPullXboxVibrations, handsAffected = CastingHand.MainHand});
            } else {
                ParentModel.SetCurrentState(HeroStateType.BowRelease);
            }
            
            if (TimeElapsedNormalized >= 1f) {
                ParentModel.SetCurrentState(HeroStateType.BowHold, 0f);
            }
        }

        protected override void OnExit(bool restarted) {
            Hero.Trigger(Awaken.TG.Main.Heroes.Hero.Events.StopProcessingAnimationSpeed, true);
        }
    }
}
