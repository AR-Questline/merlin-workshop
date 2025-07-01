using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Heroes.Combat;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Block {
    public partial class BlockExit : BlockStateBase {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.BlockExit;
        public override HeroStateType StateToEnter => UseBlockWithoutShield ? HeroStateType.BlockExitWithoutShield : HeroStateType.BlockExit;
        public override float EntryTransitionDuration => 0.15f;
        public override bool CanPerformNewAction => TimeElapsedNormalized > 0.2f;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            Hero.RemoveElementsOfType<HeroBlock>();
            PreventStaminaRegen();
        }

        protected override void OnUpdate(float deltaTime) {
            if (!CanPerformNewAction && ParentModel.BlockHeld && ParentModel.CanBlock) {
                ParentModel.SetCurrentState(ParentModel.BlockLongHeld ? HeroStateType.BlockLoop : HeroStateType.BlockStart);
                return;
            }
            
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.Idle);
            }
        }

        protected override void OnExit(bool restarted) {
            ParentModel.ApplyParryCooldown();
            DisableStaminaRegenPrevent();
        }
    }
}