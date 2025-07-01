using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public sealed partial class PoiseBreakFront : NpcPoiseBreak {
        public override ushort TypeForSerialization => SavedModels.PoiseBreakFront;

        public override NpcStateType Type => NpcStateType.PoiseBreakFront;
    }
}