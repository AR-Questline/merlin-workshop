using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcChargeLoop : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcChargeLoop;

        public override NpcStateType Type => NpcStateType.ChargeLoop;
        public override bool CanUseMovement => true;
    }
}
