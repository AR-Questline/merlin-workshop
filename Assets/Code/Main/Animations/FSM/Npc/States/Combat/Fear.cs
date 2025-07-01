using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class Fear : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.Fear;

        public override NpcStateType Type => NpcStateType.Fear;
    }
}