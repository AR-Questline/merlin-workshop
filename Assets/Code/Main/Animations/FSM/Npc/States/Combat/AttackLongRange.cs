using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class AttackLongRange : BaseAttackState {
        public override ushort TypeForSerialization => SavedModels.AttackLongRange;

        public override NpcStateType Type => NpcStateType.LongRange;
    }
}