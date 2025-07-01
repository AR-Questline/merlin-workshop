using System;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class InteractionWrapper : INpcInteractionWrapper, INpcInteraction {
        public INpcInteraction Interaction { get; private set; }

        public virtual bool CanBeInterrupted => Interaction.CanBeInterrupted;
        public bool AllowBarks => Interaction.AllowBarks;
        public virtual bool AllowDialogueAction => Interaction.AllowDialogueAction;
        public bool AllowTalk => Interaction.AllowTalk;
        public float? MinAngleToTalk => Interaction.MinAngleToTalk;
        public int Priority => Interaction.Priority;
        public bool FullyEntered => Interaction.FullyEntered;
        public bool AllowUseIK => Interaction.AllowUseIK;
        
        public InteractionWrapper(INpcInteraction interaction) {
            Interaction = interaction;
        }

        public virtual bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            return Interaction?.AvailableFor(npc, finder) ?? false;
        }
        
        public Vector3? GetInteractionPosition(NpcElement npc) {
            return Interaction.GetInteractionPosition(npc);
        }

        public Vector3 GetInteractionForward(NpcElement npc) {
            return Interaction.GetInteractionForward(npc);
        }

        public virtual InteractionBookingResult Book(NpcElement npc) {
            return Interaction.Book(npc);
        }

        public virtual void Unbook(NpcElement npc) {
            Interaction.Unbook(npc);
        }

        public virtual void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            Interaction.StartInteraction(npc, reason);
        }

        public virtual void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            Interaction.StopInteraction(npc, reason);
        }

        public virtual void ResumeInteraction(NpcElement npc, InteractionStartReason reason) {
            Interaction.ResumeInteraction(npc, reason);
        }

        public virtual void PauseInteraction(NpcElement npc, InteractionStopReason reason) {
            Interaction.PauseInteraction(npc, reason);
        }

        public event Action OnInternalEnd {
            add => Interaction.OnInternalEnd += value;
            remove => Interaction.OnInternalEnd -= value;
        }
        
        public virtual bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) {
            return Interaction.TryStartTalk(story, npc, rotateToHero);
        }

        public bool IsStopping(NpcElement npc) {
            return Interaction.IsStopping(npc);
        }

        public virtual void EndTalk(NpcElement npc, bool rotReturnToInteraction) {
            Interaction.EndTalk(npc, rotReturnToInteraction);
        }

        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) {
            return Interaction.LookAt(npc, target, lookAtOnlyWithHead);
        }
    }
}
