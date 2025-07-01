using Awaken.TG.Main.AI.Idle.Finders;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Api;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEngine;

namespace Awaken.TG.Main.AI.Idle.Interactions {
    public class GroupInteractionPart : InteractionWrapper {
        GroupInteraction ParentInteraction { get; }

        public override bool AllowDialogueAction => ParentInteraction.AllowDialogueAction && base.AllowDialogueAction;
        public override bool CanBeInterrupted => CanInteractionBeInterrupted();
        
        public GroupInteractionPart(GroupInteraction parentInteraction, INpcInteraction interaction) : base(interaction) {
            ParentInteraction = parentInteraction;
        }

        public override bool AvailableFor(NpcElement npc, IInteractionFinder finder) {
            return (!ParentInteraction.UsePartialInteractionsRequirements || base.AvailableFor(npc, finder)) 
                   && ParentInteraction.AvailableForNpc(npc, finder);
        }

        public void TriggerOnEnd() {
            if (Interaction is NpcInteractionBase npcInteraction) {
                npcInteraction.TriggerOnEnd();
            } else if (Interaction is StandInteraction standInteraction) {
                standInteraction.TriggerOnEnd();
            } else {
                Log.Important?.Error($"GroupInteractionPart: cant TriggerOnEnd on Interaction {Interaction} on Group {ParentInteraction}");
            }
        }

        public override InteractionBookingResult Book(NpcElement npc) {
            ParentInteraction.OnInteractionBooked(npc, this);
            return base.Book(npc);
        }
        
        public override void Unbook(NpcElement npc) {
            ParentInteraction.OnInteractionUnbooked(npc, this);
            base.Unbook(npc);
        }
        
        public override void StartInteraction(NpcElement npc, InteractionStartReason reason) {
            ParentInteraction.OnInteractionStarted(npc, this, reason);
            base.StartInteraction(npc, reason);
        }

        public override void StopInteraction(NpcElement npc, InteractionStopReason reason) {
            ParentInteraction.OnInteractionStopped(npc, this);
            base.StopInteraction(npc, reason);
        }

        public override void ResumeInteraction(NpcElement npc, InteractionStartReason reason) {
            ParentInteraction.OnInteractionResumed(npc, this, reason);
            base.ResumeInteraction(npc, reason);
        }

        public override void PauseInteraction(NpcElement npc, InteractionStopReason reason) {
            ParentInteraction.OnInteractionPaused(npc, this);
            base.PauseInteraction(npc, reason);
        }
        
        public override bool TryStartTalk(Story story, NpcElement npc, bool rotateToHero) {
            bool success = Interaction.TryStartTalk(story, npc, rotateToHero);
            ParentInteraction.OnTalkStarted(story, npc, this);
            return success;
        }

        public override void EndTalk(NpcElement npc, bool rotReturnToInteraction) {
            ParentInteraction.OnEndTalk(npc, this);
            base.EndTalk(npc, rotReturnToInteraction);
        }

        bool CanInteractionBeInterrupted() {
            bool canBeInterrupted = Interaction.CanBeInterrupted;
            if (canBeInterrupted) {
                return true;
            }
            if (!ParentInteraction.AllowInterruptDuringTalk) {
                return false;
            }
            if (Interaction is SimpleInteractionBase { IsTalking: true } or TalkInteraction) {
                if (ParentInteraction is StoryInteraction { Story: { HasBeenDiscarded: false } } storyInteraction) {
                    if (ParentInteraction.ForceInterruptInsteadOfRequest) {
                        storyInteraction.Story.FinishStory();
                        return true;
                    } else {
                        storyInteraction.Story.ManualInterruptRequested = true;
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
