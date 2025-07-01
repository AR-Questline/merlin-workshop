using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class AttackMagicRay : BaseAttackState {
        public override ushort TypeForSerialization => SavedModels.AttackMagicRay;

        public override NpcStateType Type => NpcStateType.MagicRay;
    }
}