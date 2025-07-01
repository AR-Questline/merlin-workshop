using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.Machines;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public abstract partial class NpcDodge : NpcAnimatorState {
        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(ParentModel is NpcOverridesFSM ? NpcStateType.None : NpcStateType.Idle);
            }
        }
    }
}