using Awaken.Utility;
using System;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Interactions;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Finders {
    public partial class InteractionBaseFinder : IInteractionFinder {
        public ushort TypeForSerialization => SavedTypes.InteractionBaseFinder;

        static readonly float[] AfterLoadSearchRanges = { 0.2f, 0.5f, 1f };
        
        [Saved] IdlePosition _position;
        [Saved] float _range;
        [Saved] public string Tag { get; private set; }
        [Saved] IdleDataElement _data;
        [Saved] bool _allowInteractionRepeat;
        [Saved] bool _allowAlreadyBooked;

        [JsonConstructor, UnityEngine.Scripting.Preserve] InteractionBaseFinder() { }
        public InteractionBaseFinder(IdlePosition position, float range, string tag, IdleDataElement data, bool allowInteractionRepeat, bool allowAlreadyBooked = true) {
            _position = position;
            _range = range;
            Tag = tag;
            _data = data;
            _allowInteractionRepeat = allowInteractionRepeat;
            _allowAlreadyBooked = allowAlreadyBooked;
        }
        
        public Vector3 GetDesiredPosition(IdleBehaviours behaviours) => _position.WorldPosition(behaviours.Location, _data);
        public float GetInteractionRadius(IdleBehaviours behaviours) => _range;

        public INpcInteraction FindInteraction(IdleBehaviours behaviours) {
            return World.Services.Get<InteractionProvider>().FindAndGetInteraction(behaviours.Npc, this, 
                _position.WorldPosition(behaviours.Location, _data), _range, _allowAlreadyBooked, _allowInteractionRepeat);
        }

        public INpcInteraction FindInteractionAfterLoad(IdleBehaviours behaviours, Type requiredType) {
            for (int i = 0; i < AfterLoadSearchRanges.Length; i++) {
                if (AfterLoadSearchRanges[i] >= _range) {
                    return null;
                }
                var searchResult = FindInteractionInRange(behaviours.Npc.Coords, AfterLoadSearchRanges[i]);
                if (searchResult != null) {
                    return searchResult;
                }
            }
            return FindInteractionInRange(_position.WorldPosition(behaviours.Location, _data), _range);

            INpcInteraction FindInteractionInRange(Vector3 position, float range) {
                if (requiredType == null) {
                    return World.Services.Get<InteractionProvider>().FindAndGetInteraction(behaviours.Npc, this, 
                        position, range, _allowAlreadyBooked, _allowInteractionRepeat);
                } else {
                    return World.Services.Get<InteractionProvider>().FindAndGetInteractionOfType(behaviours.Npc, this, 
                        position, range, _allowAlreadyBooked, _allowInteractionRepeat, requiredType);
                }
            } 
        }

        public bool CanFindInteraction(IdleBehaviours behaviours, INpcInteraction interaction, bool ignoreInteractionRequirements) {
            //Check Range
            Vector3? interactionPos = interaction.GetInteractionPosition(behaviours.Npc);
            if (interactionPos.HasValue) {
                float sqrDistance = (_position.WorldPosition(behaviours.Location, _data) - interactionPos.Value).sqrMagnitude;
                if (sqrDistance > _range * _range) {
                    return false;
                }
            }
            
            if (!_allowAlreadyBooked && interaction is NpcInteraction npcInteraction && npcInteraction.BookedBy(behaviours.Npc)) {
                return false;
            }
            
            return ignoreInteractionRequirements || interaction.AvailableFor(behaviours.Npc, this);
        }
    }
}