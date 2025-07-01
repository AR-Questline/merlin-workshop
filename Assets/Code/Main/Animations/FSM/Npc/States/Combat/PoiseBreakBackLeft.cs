using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public sealed partial class PoiseBreakBackLeft : NpcPoiseBreak {
        public override ushort TypeForSerialization => SavedModels.PoiseBreakBackLeft;

        public override NpcStateType Type => NpcStateType.PoiseBreakBackLeft;
    }
}