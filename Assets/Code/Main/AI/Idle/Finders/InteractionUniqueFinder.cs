using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public partial class InteractionUniqueFinder : DeterministicInteractionFinder {
        public override ushort TypeForSerialization => SavedTypes.InteractionUniqueFinder;

        static InteractionProvider Provider => World.Services.Get<InteractionProvider>();
        
        [Saved] string _uniqueID;
        INpcInteractionSearchable _searchable;
        
        public INpcInteractionSearchable Searchable => TryGetUniqueSearchable();
        public override INpcInteraction Interaction(NpcElement npc) => InteractionProvider.GetInteraction(npc, Searchable);

        [JsonConstructor, UnityEngine.Scripting.Preserve] InteractionUniqueFinder() { }
        public InteractionUniqueFinder(string uniqueID) {
            _uniqueID = uniqueID;
        }
        
        public override Vector3 GetDesiredPosition(IdleBehaviours behaviours) {
            return Interaction(behaviours.Npc)?.GetInteractionPosition(behaviours.Npc) ?? behaviours.Npc.Coords;
        }
        public override float GetInteractionRadius(IdleBehaviours behaviours) => 0;

        INpcInteractionSearchable TryGetUniqueSearchable() {
            if (_searchable == null || !_searchable.IsValid()) {
                _searchable = Provider.GetUniqueSearchable(_uniqueID);
            }
            return _searchable;
        }
        
        public override INpcInteraction FindInteraction(IdleBehaviours behaviours) {
            return (Searchable?.AvailableFor(behaviours.Npc, this) ?? false) ? Interaction(behaviours.Npc) : null;
        }
        
        public override bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements) {
            return InteractionUtils.AreSearchablesTheSameInteraction(behaviours.Npc, interaction, Searchable);
        }
    }
}