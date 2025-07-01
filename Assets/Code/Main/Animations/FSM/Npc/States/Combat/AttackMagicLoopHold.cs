using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class AttackMagicLoopHold : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.AttackMagicLoopHold;

        public override NpcStateType Type => NpcStateType.MagicLoopHold;
    }
}