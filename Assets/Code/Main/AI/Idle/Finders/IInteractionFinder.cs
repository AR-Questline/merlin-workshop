using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public interface IInteractionFinder {
        Vector3 GetDesiredPosition(IdleBehaviours behaviours);
        float GetInteractionRadius(IdleBehaviours behaviours);
        INpcInteraction FindInteraction(IdleBehaviours behaviours);
        bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements);

        [UnityEngine.Scripting.Preserve] public static readonly IInteractionFinder Default = new InteractionFallbackFinder();
    }

    /// <summary> InteractionFinder that finds one and only interaction without need of IdleBehaviours </summary>
    public abstract partial class DeterministicInteractionFinder : IInteractionFinder {
        public abstract ushort TypeForSerialization { get; }
        public abstract INpcInteraction Interaction(NpcElement npc);
        
        public abstract Vector3 GetDesiredPosition(IdleBehaviours behaviours);
        public abstract float GetInteractionRadius(IdleBehaviours behaviours);
        public abstract bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements);
        
        public virtual INpcInteraction FindInteraction(IdleBehaviours behaviours) {
            var interaction = Interaction(behaviours.Npc);
            return interaction.AvailableFor(behaviours.Npc, this) ? interaction : null;
        }

    }
}