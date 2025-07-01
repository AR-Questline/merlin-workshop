using Awaken.TG.Main.Animations.FSM.Heroes.Base;
using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes.Combat;

namespace Awaken.TG.Main.Animations.FSM.Heroes.States.Interactions {
    public partial class ToolInteraction : HeroAnimatorState<ToolInteractionFSM> {
        public override HeroGeneralStateType GeneralType => HeroGeneralStateType.General;
        public override HeroStateType Type => HeroStateType.ToolInteract;
        public override float EntryTransitionDuration => 0.1f;
        
        float InteractionCost => ParentModel.LightAttackCost;
        
        protected override void OnFailedFindNode() {
            ParentModel.SetCurrentState(HeroStateType.None, 0f);
        }
        
        protected override void AfterEnter(float previousStateNormalizedTime) {
            PreventStaminaRegen();
            Stamina.DecreaseBy(InteractionCost);
        }
        
        protected override void OnUpdate(float deltaTime) {
            if (TimeElapsedNormalized >= 0.75f) {
                ParentModel.SetCurrentState(HeroStateType.None);
            }
        }
        
        protected override void OnExit(bool restarted) {
            DisableStaminaRegenPrevent();
        }
    }
}
