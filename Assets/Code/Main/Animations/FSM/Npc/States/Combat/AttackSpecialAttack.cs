using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class AttackSpecialAttack : BaseAttackState {
        public override ushort TypeForSerialization => SavedModels.AttackSpecialAttack;

        public override NpcStateType Type => NpcStateType.SpecialAttack;
    }
}