using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class BlockStart : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.BlockStart;

        public override NpcStateType Type => NpcStateType.BlockStart;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.BlockHold);
            }
        }
    }
}