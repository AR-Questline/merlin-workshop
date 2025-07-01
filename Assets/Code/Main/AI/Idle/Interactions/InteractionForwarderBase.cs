using Awaken.TG.Main.Fights.NPCs;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public abstract class InteractionForwarderBase : ForwardingInteractionBase {
        [UnityEngine.Scripting.Preserve]
        public virtual INpcInteraction GetInteraction(NpcElement npc) {
            return Interaction;
        }
    }
}
