using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public partial class InteractionRoamFinder : DeterministicInteractionFinder {
        public override ushort TypeForSerialization => SavedTypes.InteractionRoamFinder;

        [Saved] IdlePosition _position;
        [Saved] float _range;
        [Saved] FloatRange _waitTime;
        [Saved] IdleDataElement _data;
        
        RoamInteraction _roamInteraction;
        RoamInteraction RoamInteraction => _roamInteraction ??= new RoamInteraction(_position, _range, _waitTime, _data);
        public override INpcInteraction Interaction(NpcElement npc) => RoamInteraction;
        
        [JsonConstructor, UnityEngine.Scripting.Preserve] InteractionRoamFinder() { }
        public InteractionRoamFinder(IdlePosition position, float range, FloatRange waitTime, IdleDataElement data) {
            _position = position;
            _range = range;
            _waitTime = waitTime;
            _data = data;
        }
        
        public override Vector3 GetDesiredPosition(IdleBehaviours behaviours) => RoamInteraction.GetRoamCenter(behaviours);
        public override float GetInteractionRadius(IdleBehaviours behaviours) => RoamInteraction.GetRoamRadius();
        public override bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements) {
            return interaction == Interaction(behaviours.Npc);
        }
    }
}