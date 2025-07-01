using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.MVC.Utils;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public partial class InteractionFollowLocationFinder : DeterministicInteractionFinder {
        public override ushort TypeForSerialization => SavedTypes.InteractionFollowLocationFinder;

        [Saved] WeakModelRef<IGrounded> _locationToFollow;
        [Saved] bool _stopOnReach;
        [Saved] float _positionRange;
        [Saved] float _exitRadiusSq;

        CommuteToLocation _commuteToLocation;
        CommuteToLocation CommuteToLocation {
            get {
                if (!_locationToFollow.IsSet || _locationToFollow.Get() is not { HasBeenDiscarded: false }) {
                    return null;
                }
                return _commuteToLocation ??= GetNewCommuteToLocation();
            }
        }

        public override INpcInteraction Interaction(NpcElement npc) => CommuteToLocation;

        [JsonConstructor, UnityEngine.Scripting.Preserve] InteractionFollowLocationFinder() { }
        
        public InteractionFollowLocationFinder(IGrounded location, bool stopOnReach, float positionRange, float exitRadiusSq) {
            _locationToFollow = new WeakModelRef<IGrounded>(location);
            _stopOnReach = stopOnReach;
            _positionRange = positionRange;
            _exitRadiusSq = exitRadiusSq;
        }

        CommuteToLocation GetNewCommuteToLocation() {
            _commuteToLocation = new CommuteToLocation(_stopOnReach);
            _commuteToLocation.Setup(_locationToFollow.Get(), _positionRange, _exitRadiusSq);
            return _commuteToLocation;
        }
        
        public override Vector3 GetDesiredPosition(IdleBehaviours behaviours) => behaviours.Npc.Coords;
        public override float GetInteractionRadius(IdleBehaviours behaviours) => 0;
        public override bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements) {
            return interaction == Interaction(behaviours.Npc);
        }
    }
}