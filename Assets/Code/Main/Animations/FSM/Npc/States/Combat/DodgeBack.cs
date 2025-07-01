using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class DodgeBack : NpcDodge {
        public override ushort TypeForSerialization => SavedModels.DodgeBack;

        public override NpcStateType Type => NpcStateType.DodgeBack;
    }
}