using Awaken.Utility;
using System;
using Awaken.TG.Main.AI.Idle.Behaviours;
using Awaken.TG.Main.AI.Idle.Data.Runtime;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.AI.Idle.Interactions.Saving;
using Awaken.TG.Main.AI.Movement;
using Awaken.TG.Main.AI.Movement.Controllers;
using Awaken.TG.Main.AI.Movement.States;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using JetBrains.Annotations;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class RoamInteraction : INpcInteraction, ISavableInteraction {
        readonly IdlePosition _position;
        readonly Patrol _patrol;
        [CanBeNull] readonly IdleDataElement _data;

        public bool CanBeInterrupted => true;
        public bool AllowBarks => true;
        public bool AllowDialogueAction => AllowTalk;
        public bool AllowTalk => true;
        public float? MinAngleToTalk => null;
        public int Priority => 0;
        public bool FullyEntered => true;

        public RoamInteraction(IdlePosition position, float range, FloatRange waitTime, IdleDataElement data) {
            _position = position;
            _patrol = new Patrol(CharacterPlace.Default, range, VelocityScheme.Walk, waitTime);
            _data = data;
        }
        
        public Vector3? GetInteractionPosition(NpcElement npc) => null;
        public Vector3 GetInteractionForward(NpcElement npc) => npc.Forward();

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => true;

        public InteractionBookingResult Book(NpcElement npc) => InteractionBookingResult.ProperlyBooked;
        public void Unbook(NpcElement npc) { }

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            if (TryLoadAndSetupSavedData(npc, reason)) {
                npc.Movement.ChangeMainState(_patrol);
                return;
            }
            
            var behaviours = npc.Behaviours;
            _patrol.UpdatePlace(new CharacterPlace(GetRoamCenter(behaviours), behaviours.PositionRange));
            npc.Movement.ChangeMainState(_patrol);
            _patrol.SelectNewRandomDestination();
        }
        
        public bool TryLoadAndSetupSavedData(NpcElement npc, InteractionStartReason startReason) {
            if (startReason is not InteractionStartReason.NPCReactivatedFromGameLoad) {
                return false;
            }
            var behaviours = npc.Behaviours;
            if (behaviours.TryGetSavedInteractionData() is RoamInteractionSavedData data) {
                _patrol.UpdatePlace(new CharacterPlace(GetRoamCenter(behaviours), behaviours.PositionRange), new CharacterPlace(data.position, behaviours.PositionRange));
                return true;
            }
            return false;
        }
        
        public void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            if (reason == InteractionStopReason.Death) return; 
            npc?.Movement?.ResetMainState(_patrol);
        }
        
        public SavedInteractionData SaveData(NpcElement npc) {
            return new RoamInteractionSavedData() {
                    position = _patrol.CurrentRandomPlace
                };
        }
        
        public bool IsStopping(NpcElement npc) => false;

        public event Action OnInternalEnd { add { } remove { } }
        
        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;

        public Vector3 GetRoamCenter(IdleBehaviours behaviours) => _position.WorldPosition(behaviours.Location, _data);
        public float GetRoamRadius() => _patrol.Radius;
    }

    [Serializable]
    public partial class RoamInteractionSavedData : SavedInteractionData {
        public override ushort TypeForSerialization => SavedTypes.RoamInteractionSavedData;

        [Saved] internal Vector3 position;
        
        public override INpcInteraction TryToGetInteraction(IdleBehaviours behaviours) {
            if (behaviours.CurrentFinder is InteractionRoamFinder roamFinder) {
                return roamFinder.Interaction(behaviours.Npc);
            }
            return null;
        }
    }
}