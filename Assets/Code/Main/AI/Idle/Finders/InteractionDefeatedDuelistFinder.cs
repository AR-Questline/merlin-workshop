using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public partial class InteractionDefeatedDuelistFinder : DeterministicInteractionFinder {
        public override ushort TypeForSerialization => SavedTypes.InteractionDefeatedDuelistFinder;

        [Saved] Vector3 _interactionPosition;
        [Saved] ARAssetReference _animations;
        [Saved] bool _canBeTalkedTo;

        InteractionDefeatedDuelist _defeatedInteraction;
        InteractionDefeatedDuelist DefeatedInteraction => _defeatedInteraction ??= new InteractionDefeatedDuelist(_animations, _canBeTalkedTo);
        public override INpcInteraction Interaction(NpcElement npc) => DefeatedInteraction;

        [JsonConstructor, UnityEngine.Scripting.Preserve] InteractionDefeatedDuelistFinder() { }
        public InteractionDefeatedDuelistFinder(Vector3 interactionPosition, ARAssetReference animations, bool canBeTalkedTo) {
            _interactionPosition = interactionPosition;
            _animations = animations;
            _canBeTalkedTo = canBeTalkedTo;
        }
        
        public override Vector3 GetDesiredPosition(IdleBehaviours behaviours) => _interactionPosition;
        public override float GetInteractionRadius(IdleBehaviours behaviours) => 2;
        public override bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements) {
            return interaction == Interaction(behaviours.Npc);
        }
    }
}