using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Bow {
    public partial class BowHold : HeroAnimatorState<BowFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.BowDraw;
        public override HeroStateType Type => HeroStateType.BowHold;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            Hero.Trigger(ICharacter.Events.OnRangedWeaponFullyDrawn, Hero);
            
            if (ParentModel.HeroBow != null) {
                ParentModel.HeroBow.OnHoldBow();
            }
        }

        protected override void OnUpdate(float deltaTime) {
            float staminaTick = ParentModel.StatsItemStats ? ParentModel.StatsItemStats.HoldItemCostPerTick.ModifiedValue * Hero.HeroStats.ItemStaminaCostMultiplier : 0;
            
            if (StaminaUsedUpEffect.TryDecreaseContinuously(staminaTick, deltaTime)) {
                Hero.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData{effects = GameConstants.Get.bowHoldXboxVibrations, handsAffected = CastingHand.MainHand});
            } else {
                ParentModel.SetCurrentState(HeroStateType.BowRelease);
            }
        }
    }
}
