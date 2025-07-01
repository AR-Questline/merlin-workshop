using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcChargeInterrupted : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcChargeInterrupted;

        public override NpcStateType Type => NpcStateType.ChargeInterrupt;
        public override bool CanUseMovement => false;
        
        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
    }
}
