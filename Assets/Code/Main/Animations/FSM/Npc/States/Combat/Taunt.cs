using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class Taunt : BaseCombatState {
        public override ushort TypeForSerialization => SavedModels.Taunt;

        public override NpcStateType Type => NpcStateType.Taunt;
    }
}