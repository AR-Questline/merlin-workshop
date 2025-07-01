using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcStaggerLoop : NpcAnimatorState {
        public override ushort TypeForSerialization => SavedModels.NpcStaggerLoop;

        public override NpcStateType Type => NpcStateType.StaggerLoop;
        public override bool ResetMovementSpeed => true;
    }
}