using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class InteractionForwarder : InteractionForwarderBase {
        [SerializeField] NpcInteractionBase interaction;
        public override INpcInteraction Interaction => interaction;
    }
}
