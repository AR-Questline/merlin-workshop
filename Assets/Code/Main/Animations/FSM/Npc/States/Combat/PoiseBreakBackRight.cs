using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public sealed partial class PoiseBreakBackRight : NpcPoiseBreak {
        public override ushort TypeForSerialization => SavedModels.PoiseBreakBackRight;

        public override NpcStateType Type => NpcStateType.PoiseBreakBackRight;
    }
}