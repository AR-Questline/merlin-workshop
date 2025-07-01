using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Animations;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Bow {
    public partial class BowRelease : HeroAnimatorState<BowFSM>, IStateWithModifierAttackSpeed {
        const float ReleaseArrowAfter = 0.05f;
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.BowRelease;
        public override bool CanPerformNewAction => TimeElapsedNormalized > 0.75f;
        
        public float AttackSpeed => ParentModel.GetAttackSpeed(false);

        bool _projectileReleased;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _projectileReleased = false;
            PreventStaminaRegen();
            ParentModel.EndSlowModifier();
            Hero.FoV.ApplyBowShootZoom(1.1f);
            
            if (ParentModel.HeroBow != null) {
                ParentModel.HeroBow.OnReleaseBow();
            }
        }

        protected override void OnUpdate(float deltaTime) {
            if (CurrentState != null) {
                CurrentState.Speed = AttackSpeed;
            }
            
            if (!_projectileReleased && TimeElapsedNormalized >= ReleaseArrowAfter) {
                ParentModel.FireProjectile();
                Hero.Trigger(BowFSM.Events.OnBowRelease, true);
                Hero.Current.Trigger(GamepadEffects.Events.TriggerVibrations, new TriggersVibrationData{effects = GameConstants.Get.bowReleaseXboxVibrations, handsAffected = CastingHand.MainHand});
                Hero.FoV.EndBowShootZoom();
                _projectileReleased = true;
            }

            if (Hero.TppActive) {
                if (TimeElapsedNormalized >= 0.75f) {
                    ParentModel.SetCurrentState(HeroStateType.Idle);
                }
                return;
            }

            if (TimeElapsedNormalized >= 1f) {
                ParentModel.SetCurrentState(HeroStateType.Idle, 0f);
            }
        }

        protected override void OnExit(bool restarted) {
            DisableStaminaRegenPrevent();
            if (ParentModel.HeroBow != null) {
                ParentModel.HeroBow.OnBowIdle();
            }
        }
    }
}
