using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class BlockImpact : BaseCombatState {
        public override ushort TypeForSerialization => SavedModels.BlockImpact;

        public override NpcStateType Type => NpcStateType.BlockImpact;

        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.BlockHold);
            }
        }
    }
}