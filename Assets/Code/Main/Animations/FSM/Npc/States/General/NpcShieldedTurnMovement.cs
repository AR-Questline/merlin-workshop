using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcShieldedTurnMovement : NpcTurnMovement {
        public override ushort TypeForSerialization => SavedModels.NpcShieldedTurnMovement;

        public override NpcStateType Type => NpcStateType.ShieldManTurnMovement;
        protected override NpcStateType StateToEnter => NpcStateType.ShieldManMovement;
        protected override NpcStateType StateToReturnTo => NpcStateType.ShieldManMovement;
    }
}