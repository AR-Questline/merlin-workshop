using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class DodgeRight : NpcDodge {
        public override ushort TypeForSerialization => SavedModels.DodgeRight;

        public override NpcStateType Type => NpcStateType.DodgeRight;
    }
}