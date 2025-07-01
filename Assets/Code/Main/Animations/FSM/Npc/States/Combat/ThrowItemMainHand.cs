using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class ThrowItemMainHand : BaseAttackState {
        public override ushort TypeForSerialization => SavedModels.ThrowItemMainHand;

        public override NpcStateType Type => NpcStateType.ThrowItemMainHand;
    }
}