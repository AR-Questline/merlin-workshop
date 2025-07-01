using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class GuardInteraction : NpcInteraction {
        SnapToPositionAndRotate _snapToPosition;
        
        protected override void OnStart(NpcElement npc, InteractionStartReason reason) {
            Vector3 position = GetInteractionPosition(npc) ?? npc.Coords;
            _snapToPosition = new SnapToPositionAndRotate(position, GetInteractionForward(npc), gameObject);
           npc.Movement.ChangeMainState(_snapToPosition);
        }

        protected override void OnEnd(NpcElement npc, InteractionStopReason reason) {
            npc?.Movement?.ResetMainState(_snapToPosition);
        }
    }
}