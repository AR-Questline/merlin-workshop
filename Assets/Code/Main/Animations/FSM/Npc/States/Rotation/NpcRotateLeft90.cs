using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Rotation {
    public partial class NpcRotateLeft90 : NpcRotate {
        public override ushort TypeForSerialization => SavedModels.NpcRotateLeft90;

        public override NpcStateType Type => NpcStateType.RotateLeft90;
    }
}