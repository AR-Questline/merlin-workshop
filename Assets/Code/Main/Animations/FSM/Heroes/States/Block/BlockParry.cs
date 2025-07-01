using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.Utility.Audio;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Block {
    public partial class BlockParry : BlockStateBase {
        const float BaseParryDuration = 0.050f;
        const float ExitParryAfter = 0.9f;
        
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.BlockParry;
        public override HeroStateType StateToEnter => UseBlockWithoutShield ? HeroStateType.BlockParryWithoutShield : HeroStateType.BlockParry;

        public override bool CanPerformNewAction => _canExitFromParry;
        bool _canExitFromParry;

        protected override void OnInitialize() {
            ParentModel.ParentModel.HealthElement.ListenTo(HealthElement.Events.OnDamageParried, OnDamageParried, this);
        }

        protected override void AfterEnter(float previousStateNormalizedTime) {
            _canExitFromParry = false;
            PreventStaminaRegen();
            ParentModel.ResetAttackProlong();
            Hero.RemoveElementsOfType<HeroBlock>();
            
            float parryDuration = BaseParryDuration + Hero.HeroStats.ParryWindowBonus.ModifiedValue;
            HeroParry.Parry(Hero, new TimeDuration(parryDuration));
            HeroBlock.GetBlockingWeapon(Hero)?.PlayAudioClip(ItemAudioType.ParrySwing);

            Hero.Trigger(HeroHealthElement.Events.HeroParryPostponeWindowEnded, true);
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= ExitParryAfter) {
                ParentModel.SetCurrentState(HeroStateType.Idle, 0.1f);
            }
        }

        protected override void OnExit(bool restarted) {
            _canExitFromParry = false;
            DisableStaminaRegenPrevent();
        }

        void OnDamageParried() {
            if (ParentModel.IsCurrentState(Type)) {
                _canExitFromParry = true;
            }
        }
    }
}