using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Rotation {
    public partial class NpcRotateRight45 : NpcRotate {
        public override ushort TypeForSerialization => SavedModels.NpcRotateRight45;

        public override NpcStateType Type => NpcStateType.RotateRight45;
    }
}