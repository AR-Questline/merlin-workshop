using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class PreventDamageStateInterrupted : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.PreventDamageStateInterrupted;

        public override NpcStateType Type => NpcStateType.PreventDamageInterrupt;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
    }
}
