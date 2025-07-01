using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Rotation {
    public partial class NpcRotateLeft45 : NpcRotate {
        public override ushort TypeForSerialization => SavedModels.NpcRotateLeft45;

        public override NpcStateType Type => NpcStateType.RotateLeft45;
    }
}