using System;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Fights.NPCs.Presences;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class StayInAbyssInteraction : INpcInteraction {
        bool _movingToAbyss;
        
        public bool CanBeInterrupted => true;
        public bool AllowBarks => false;
        public bool AllowDialogueAction => AllowTalk;
        public bool AllowTalk => false;
        public float? MinAngleToTalk => null;
        public int Priority => 99;
        public bool FullyEntered => true;

        public Vector3? GetInteractionPosition(NpcElement npc) => null;
        public Vector3 GetInteractionForward(NpcElement npc) => Vector3.forward;

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => true;

        public InteractionBookingResult Book(NpcElement npc) => InteractionBookingResult.ProperlyBooked;
        public void Unbook(NpcElement npc) { }

        public void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            if (NpcPresence.InAbyss(npc.Coords)) {
                return;
            }
            npc.Movement.Controller.MoveToAbyss();
        }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            if (reason is InteractionStopReason.MySceneUnloading or InteractionStopReason.NPCPresenceDisabled) {
                return;
            }
            if (reason == InteractionStopReason.Death) {
                npc.Movement?.Controller.AbortMoveToAbyss();
                return;
            }
            if (npc?.HasBeenDiscarded ?? true) {
                return;
            }
            
            npc.ParentModel.SetInteractability(LocationInteractability.Active);
            npc.Movement?.Controller.AbortMoveToAbyss();
        }

        public event Action OnInternalEnd { add { } remove { } }
        public bool IsStopping(NpcElement npc) => false;
        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;
    }
}