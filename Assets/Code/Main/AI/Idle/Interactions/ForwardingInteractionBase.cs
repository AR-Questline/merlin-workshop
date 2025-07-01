using System;
using System.Collections.Generic;
using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Utility.Animations.Gestures;
using Awaken.Utility;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public abstract class ForwardingInteractionBase : MonoBehaviour, INpcInteraction {
        public abstract INpcInteraction Interaction { get; }

        public bool CanBeInterrupted => Interaction.CanBeInterrupted;
        public bool AllowBarks => Interaction.AllowBarks;
        public bool AllowDialogueAction => Interaction.AllowDialogueAction;
        public bool AllowTalk => Interaction.AllowTalk;
        public float? MinAngleToTalk => Interaction.MinAngleToTalk;
        public int Priority => Interaction.Priority;
        protected bool IsTalking { get; private set; }
        public bool FullyEntered => Interaction.FullyEntered;
        public bool AllowUseIK => Interaction.AllowUseIK;
        public GesturesSerializedWrapper Gestures => Interaction.Gestures;

        public void GetAllInteractions(List<INpcInteraction> interactions) {
            interactions.Add(Interaction);
        }

        public virtual bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            return Interaction?.AvailableFor(npc, finder) ?? false;
        }
        
        public virtual Vector3? GetInteractionPosition(NpcElement npc) {
            return Interaction.GetInteractionPosition(npc);
        }

        public virtual Vector3 GetInteractionForward(NpcElement npc) {
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

        public virtual event Action OnInternalEnd {
            add => Interaction.OnInternalEnd += value;
            remove => Interaction.OnInternalEnd -= value;
        }

        public bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) {
            IsTalking = Interaction.TryStartTalk(story, npc, rotateToHero);
            return IsTalking;
        }

        public virtual bool IsStopping(NpcElement npc) {
            return Interaction.IsStopping(npc);
        }

        public void EndTalk(NpcElement npc, bool rotReturnToInteraction) {
            Interaction.EndTalk(npc, rotReturnToInteraction);
            IsTalking = false;
        }

        public bool LookAt(NpcElement npc, GroundedPosition target, bool lookAtOnlyWithHead) {
            return Interaction.LookAt(npc, target, lookAtOnlyWithHead);
        }
        
#if UNITY_EDITOR
        void Reset() {
            gameObject.layer = RenderLayers.AIInteractions;
        }
#endif
    }
}
