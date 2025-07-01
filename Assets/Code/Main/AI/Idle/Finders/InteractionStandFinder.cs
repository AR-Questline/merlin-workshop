using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public partial class InteractionStandFinder : DeterministicInteractionFinder {
        public override ushort TypeForSerialization => SavedTypes.InteractionStandFinder;

        [Saved] IdlePosition _position;
        [Saved] IdlePosition _forward;
        [Saved] IdleDataElement _data;

        StandInteraction _standInteraction;
        StandInteraction StandInteraction => _standInteraction ??= new StandInteraction(_position, _forward, _data);
        public override INpcInteraction Interaction(NpcElement npc) => StandInteraction;

        [JsonConstructor, UnityEngine.Scripting.Preserve] InteractionStandFinder() { }
        public InteractionStandFinder(IdlePosition position, IdlePosition forward, IdleDataElement data) {
            _position = position;
            _forward = forward;
            _data = data;
        }
        
        public override Vector3 GetDesiredPosition(IdleBehaviours behaviours) => StandInteraction.GetPosition(behaviours);
        public override float GetInteractionRadius(IdleBehaviours behaviours) => 0;
        public override bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements) {
            return interaction == Interaction(behaviours.Npc);
        }
    }
}