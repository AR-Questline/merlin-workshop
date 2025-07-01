using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class Rest : BaseCombatState {
        public override ushort TypeForSerialization => SavedModels.Rest;

        public override NpcStateType Type => NpcStateType.Rest;
    }
}