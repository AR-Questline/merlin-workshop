using Awaken.TG.Main.Animations.FSM.Npc.Base;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Base {
    public abstract partial class BaseCombatState : NpcAnimatorState {
        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= ParentModel.ExitDurationFromAttackAnimations) {
                ParentModel.SetCurrentState(NpcStateType.Wait, 0.4f);
            }
        }
    }
}