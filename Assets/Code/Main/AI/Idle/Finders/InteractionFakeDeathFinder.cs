using Awaken.TG.Assets;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public partial class InteractionFakeDeathFinder : DeterministicInteractionFinder {
        public override ushort TypeForSerialization => SavedTypes.InteractionFakeDeathFinder;

        [Saved] Vector3 _lastPosition;
        [Saved] float _startDuration;
        [Saved] float _endDuration;
        [Saved] bool _forceChangeIntoGhost;
        [Saved] bool _ifForcedStayInGhost;
        [Saved] ARAssetReference _animations;

        FakeDeathInteraction _fakeDeathInteraction;
        FakeDeathInteraction FakeDeathInteraction => _fakeDeathInteraction ??= new FakeDeathInteraction(_lastPosition, _startDuration, _endDuration, _animations, _forceChangeIntoGhost, _ifForcedStayInGhost);
        public override INpcInteraction Interaction(NpcElement npc) => FakeDeathInteraction;


        [JsonConstructor, UnityEngine.Scripting.Preserve] InteractionFakeDeathFinder() { }
        public InteractionFakeDeathFinder(Vector3 lastPosition, float startDuration, float endDuration, ARAssetReference animations, bool forceChangeIntoGhost, bool ifForcedStayInGhost) {
            _lastPosition = lastPosition;
            _startDuration = startDuration;
            _endDuration = endDuration;
            _animations = animations;
            _forceChangeIntoGhost = forceChangeIntoGhost;
            _ifForcedStayInGhost = ifForcedStayInGhost;
        }
        
        public override Vector3 GetDesiredPosition(IdleBehaviours behaviours) => _lastPosition;
        public override float GetInteractionRadius(IdleBehaviours behaviours) => 0;
        public override bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements) {
            return interaction == Interaction(behaviours.Npc);
        }
    }
}