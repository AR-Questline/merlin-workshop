using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class DodgeLeft : NpcDodge {
        public override ushort TypeForSerialization => SavedModels.DodgeLeft;

        public override NpcStateType Type => NpcStateType.DodgeLeft;
    }
}