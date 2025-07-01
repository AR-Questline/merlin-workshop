using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcChargeExit : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcChargeExit;

        public override NpcStateType Type => NpcStateType.ChargeExit;
        public override bool CanUseMovement => true;
        
        protected override void OnUpdate(float deltaTime) {
            if (RemainingDuration <= 0.3f) {
                ParentModel.SetCurrentState(NpcStateType.Idle);
            }
        }
    }
}
