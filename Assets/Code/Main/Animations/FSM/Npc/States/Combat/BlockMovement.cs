using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class BlockMovement : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.BlockMovement;

        public override NpcStateType Type => NpcStateType.BlockMovement;
        
        protected override void OnUpdate(float deltaTime) {
            if (NpcAnimancer.MovementSpeed < 0.05f) {
                ParentModel.SetCurrentState(NpcStateType.BlockHold);
            }
        }
    }
}