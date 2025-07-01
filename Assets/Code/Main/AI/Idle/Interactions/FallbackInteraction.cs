using System;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class FallbackInteraction : INpcInteraction {
        public bool CanBeInterrupted => true;
        public bool AllowBarks => true;
        public bool AllowDialogueAction => AllowTalk;
        public bool AllowTalk => true;
        public float? MinAngleToTalk => null;
        public int Priority => 0;
        public bool FullyEntered => true;

        public Vector3? GetInteractionPosition(NpcElement npc) => null;
        public Vector3 GetInteractionForward(NpcElement npc) => Vector3.zero;

        public bool AvailableFor(NpcElement npc, IInteractionFinder finder) => true;

        public InteractionBookingResult Book(NpcElement npc) => InteractionBookingResult.ProperlyBooked;
        public void Unbook(NpcElement npc) { }
        
        public void StartInteraction(NpcElement npc, InteractionStartReason reason) { }

        public void StopInteraction(NpcElement npc, InteractionStopReason reason) { }
        public bool IsStopping(NpcElement npc) => false;
        public event Action OnInternalEnd { add { } remove { } }
        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) => false;
        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) { }
        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) => false;
    }
}