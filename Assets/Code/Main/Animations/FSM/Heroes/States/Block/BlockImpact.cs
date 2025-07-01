using Awaken.TG.Main.Animations.FSM.Heroes.Base;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Block {
    public partial class BlockImpact : BlockStateBase {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.Block;
        public override HeroStateType Type => HeroStateType.BlockImpact;
        public override HeroStateType StateToEnter => UseBlockWithoutShield ? HeroStateType.BlockImpactWithoutShield : HeroStateType.BlockImpact;
        public override bool CanPerformNewAction => false;
        public override float EntryTransitionDuration => 0.05f;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            PreventStaminaRegen();
        }

        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(ParentModel.BlockHeld ? HeroStateType.BlockLoop : HeroStateType.Idle, 0.2f);
            }
        }

        protected override void OnExit(bool restarted) {
            DisableStaminaRegenPrevent();
        }
    }
}