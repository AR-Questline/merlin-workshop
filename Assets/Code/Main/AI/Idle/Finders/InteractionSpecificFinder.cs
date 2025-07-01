using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public class InteractionSpecificFinder : IInteractionFinder {
        INpcInteraction _interaction;
        
        public InteractionSpecificFinder(INpcInteraction interaction) {
            _interaction = interaction;
        }
        
        public Vector3 GetDesiredPosition(IdleBehaviours behaviours) => _interaction.GetInteractionPosition(behaviours.Npc) ?? behaviours.Npc.Coords;
        public float GetInteractionRadius(IdleBehaviours behaviours) => 0;
        public INpcInteraction FindInteraction(IdleBehaviours behaviours) => _interaction;
        public bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements) {
            return interaction == _interaction;
        }
    }
}