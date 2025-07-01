using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public sealed partial class PoiseBreakBack : NpcPoiseBreak {
        public override ushort TypeForSerialization => SavedModels.PoiseBreakBack;

        public override NpcStateType Type => NpcStateType.PoiseBreakBack;
    }
}