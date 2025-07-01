using Animancer;
using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Settings.Accessibility;
using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Utility.Animations;
using Awaken.TG.MVC;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Melee {
    public partial class HeavyAttackWait : HeroAnimatorState<MeleeFSM> {
        const float StaminaCostMultiplierWhenSprinting = 3;
        
        MixerState<Vector2> _mixerState;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.HeavyAttack;
        public override HeroStateType Type => HeroStateType.HeavyAttackWait;
        public override HeroStateType StateToEnter => ParentModel.HeavyAttackIndex <= 1
            ? HeroStateType.HeavyAttackWait
            : HeroStateType.HeavyAttackWaitAlternate;
        public override bool UsesActiveLayerMask => true;
        protected override bool HeadBobbingDependent => true;
        protected float BlendSpeed => AnimancerUtils.BlendTreeBlendSpeed();
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            _mixerState = CurrentState as MixerState<Vector2>;

            ParentModel.Stamina.DecreaseBy(ParentModel.StatsItemStats.HeavyAttackHoldCostPerTick * Hero.GetDeltaTime() * Hero.HeroStats.ItemStaminaCostMultiplier);
            Hero.Trigger(ICharacter.Events.OnHeavyAttackHoldStarted, Hero);
        }

        protected override void OnUpdate(float deltaTime) {
            float staminaCost = ParentModel.StatsItemStats.HeavyAttackHoldCostPerTick 
                                * Hero.HeroStats.ItemStaminaCostMultiplier
                                * deltaTime;
            if (Hero.IsSprinting) {
                staminaCost *= StaminaCostMultiplierWhenSprinting;
            }
            
            if (_mixerState != null) {
                var mixerParam = new Vector2(Hero.RelativeVelocity.y, Hero.RelativeVelocity.x);
                _mixerState.Parameter = Vector2.MoveTowards(_mixerState.Parameter, mixerParam, BlendSpeed * deltaTime);
            }
            
            ParentModel.Stamina.DecreaseBy(staminaCost);
            Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData{effects = GameConstants.Get.heavyAttackWaitXboxVibrations, handsAffected = GetHandForMeleeVibrations});
        }
    }
}