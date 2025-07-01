using System;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Utility.Animations.Gestures;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public interface INpcInteraction : INpcInteractionSearchable {
        bool CanBeInterrupted { get; }
        bool CanBePushedFrom => CanBeInterrupted;
        bool AllowBarks { get; }
        bool AllowDialogueAction { get; }
        bool AllowTalk { get; }
        bool AllowGlancing => AllowTalk;
        float? MinAngleToTalk { get; }
        int Priority { get; }
        bool FullyEntered { get; }
        bool AllowUseIK => true;
        bool CanUseTurnMovement => true;
        GesturesSerializedWrapper Gestures => null;

        Vector3? GetInteractionPosition(NpcElement npc);
        Vector3 GetInteractionForward(NpcElement npc);
        
        InteractionBookingResult Book(NpcElement npc);
        void Unbook(NpcElement npc);

        void StartInteraction(NpcElement npc, InteractionStartReason reason);
        void StopInteraction(NpcElement npc, InteractionStopReason reason);

        void ResumeInteraction(NpcElement npc, InteractionStartReason reason) => StartInteraction(npc, reason);
        void PauseInteraction(NpcElement npc, InteractionStopReason reason) => StopInteraction(npc, reason);

        event Action OnInternalEnd;

        bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero);
        bool IsStopping(NpcElement npc);
        void EndTalk(NpcElement npc, bool rotReturnToInteraction);
        
        /// <summary> rotates npc and returns if SNpcLookAt need wait for it </summary>
        bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead);
    }
    
    public enum InteractionStartReason : byte {
        ChangeInteraction,
        ResumeInteraction,
        NPCActivated,
        NPCPresenceDisabled,
        NPCDeactivated,
        NPCReactivatedFromGameLoad,
        NPCStartedCombat,
        NPCEndedCombat,
        InteractionFastSwap,
    }
    
    public enum InteractionStopReason : byte {
        ChangeInteraction,
        StoppedIdling,
        StoppedIdlingInstant,
        Death,
        ComebackFromScene,
        MySceneUnloading,
        NPCReactivated,
        NPCPresenceDisabled,
        NPCDeactivated,
        NPCStartedCombat,
        InteractionFastSwap,
    }

    public enum InteractionBookingResult : byte {
        CannotBeBooked,
        ProperlyBooked,
        AlreadyBookedBySameNpc,
        AlreadyBookedByOtherNpc,
    }
}