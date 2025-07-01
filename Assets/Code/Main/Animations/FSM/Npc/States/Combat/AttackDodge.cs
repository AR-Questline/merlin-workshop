using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class AttackDodge : BaseAttackState {
        public override ushort TypeForSerialization => SavedModels.AttackDodge;

        public override NpcStateType Type => NpcStateType.DodgeAttack;
    }
}