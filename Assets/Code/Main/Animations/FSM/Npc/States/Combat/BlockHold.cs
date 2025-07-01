using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public sealed partial class BlockHold : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.BlockHold;

        public override NpcStateType Type => NpcStateType.BlockHold;
    }
}