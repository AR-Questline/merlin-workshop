using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Melee {
    public partial class HeavyAttackStart : HeroAnimatorState<MeleeFSM>, IStateWithModifierAttackSpeed {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.HeavyAttack;
        public override HeroStateType Type => HeroStateType.HeavyAttackStart;
        public override HeroStateType StateToEnter => ParentModel.HeavyAttackIndex <= 1
            ? HeroStateType.HeavyAttackStart
            : HeroStateType.HeavyAttackStartAlternate;
        public override bool CanPerformNewAction => TimeElapsedNormalized >= 0.75f;
        public float AttackSpeed => ParentModel.GetAttackSpeed(true);
        public override bool UsesActiveLayerMask => true;
        
        protected override bool BeforeEnter(out HeroStateType desiredState) {
            if (ParentModel.HeavyAttackIndex == -1) {
                ParentModel.HeavyAttackIndex = 1;
            } else {
                ParentModel.HeavyAttackIndex = ParentModel.HeavyAttackIndex > 1 ? 1 : 2;
            }
            return base.BeforeEnter(out desiredState);
        }
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            ParentModel.ResetAttackProlong();
            PreventStaminaRegen();
        }

        protected override void OnUpdate(float deltaTime) {
            if (CurrentState != null) {
                CurrentState.Speed = AttackSpeed;
            }

            Hero.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData {
                effects = TimeElapsedNormalized > 0.5f ? 
                GameConstants.Get.heavyAttackStartFirstHalfXboxVibrations : 
                GameConstants.Get.heavyAttackStartSecondHalfXboxVibrations, 
                handsAffected = GetHandForMeleeVibrations});
            
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.HeavyAttackWait);
            }
        }

        protected override void OnExit(bool restarted) {
            DisableStaminaRegenPrevent();
        }
    }
}