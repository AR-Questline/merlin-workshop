using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.TG.Main.Animations.FSM.Npc.States.Base;
using Awaken.Utility;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.Combat {
    public partial class NpcWait : BaseCombatState {
        public override ushort TypeForSerialization => SavedModels.NpcWait;

        public override NpcStateType Type => NpcStateType.Wait;
        public override bool CanReEnter => false;

        protected override void AfterEnter(float previousStateNormalizedTime) {
            Npc.Movement.Controller.FinalizeMovement();
        }
    }
}