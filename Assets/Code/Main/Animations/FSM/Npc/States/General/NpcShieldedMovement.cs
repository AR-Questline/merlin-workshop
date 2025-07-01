using Animancer;
using Awaken.TG.Main.Animations.FSM.Npc.Base;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.Animations.FSM.Npc.States.General {
    public partial class NpcShieldedMovement : NpcMovement {
        public override ushort TypeForSerialization => SavedModels.NpcShieldedMovement;

        public override NpcStateType Type => NpcStateType.ShieldManMovement;
        protected override NpcStateType StateToEnter => Type;

        protected override bool CanLeaveToIdle => false;
        protected override NpcStateType TurningState => NpcStateType.ShieldManTurnMovement;
    }
}