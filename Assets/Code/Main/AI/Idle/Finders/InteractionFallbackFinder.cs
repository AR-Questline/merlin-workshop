using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public partial class InteractionFallbackFinder : DeterministicInteractionFinder {
        public override ushort TypeForSerialization => SavedTypes.InteractionFallbackFinder;
        
        static readonly FallbackInteraction Fallback = new();
        public override INpcInteraction Interaction(NpcElement npc) => Fallback;
        public override Vector3 GetDesiredPosition(IdleBehaviours behaviours) => behaviours.Npc.Coords;
        public override float GetInteractionRadius(IdleBehaviours behaviours) => 0;
        public override bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements) {
            return interaction == Interaction(behaviours.Npc);
        }
    }
}