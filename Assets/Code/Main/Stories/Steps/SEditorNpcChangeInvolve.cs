using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Interfaces;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("NPC/NPC: Change Involve")]
    public class SEditorNpcChangeInvolve : EditorStep, IStoryActorRef {
        [Tooltip("Works only for actors already added to Story.")]
        public ActorRef actorRef;
        [Tooltip("Should the NPC be invulnerable during the story?")]
        public bool invulnerability = true;
        [Tooltip("Should the NPC be involved in Story (play story loop animation and look at their dialogue target)?")]
        public bool involve = true;
        [HideIf(nameof(involve)), Tooltip("Should the NPC rotate to interaction rotation or stay rotated as he is now?")]
        public bool rotReturnToInteraction = true;
        [ShowIf(nameof(involve)), Tooltip("Should the NPC instantly rotate to the Hero?")]
        public bool rotToHero = true;
        [ShowIf(nameof(involve)), Tooltip("Should the NPC be forced to exit interaction?")]
        public bool forceExitInteraction;
        [Tooltip("Should the story wait for NPC involvement? If not, NPC might not be ready for next steps.")]
        public bool waitForInvolvement = true;
        
        public ActorRef[] ActorRef => new[] { actorRef };

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SNpcChangeInvolve {
                actorRef = actorRef,
                invulnerability = invulnerability,
                involve = involve,
                rotReturnToInteraction = rotReturnToInteraction,
                rotToHero = rotToHero,
                forceExitInteraction = forceExitInteraction,
                waitForInvolvement = waitForInvolvement
            };
        }
    }

    public partial class SNpcChangeInvolve : StoryStep {
        public ActorRef actorRef;
        public bool invulnerability = true;
        public bool involve = true;
        public bool rotReturnToInteraction = true;
        public bool rotToHero = true;
        public bool forceExitInteraction;
        public bool waitForInvolvement = true;
        
        public override StepResult Execute(Story story) {
            var stepResult = new StepResult();
            AddLocationsToStory(story, stepResult).Forget();
            return stepResult;
        }

        async UniTaskVoid AddLocationsToStory(Story api, StepResult result) {
            Actor actor = actorRef.Get();
            if (StoryUtils.FindCharacter(api, actor, false) is NpcElement npc) {
                var task = api.SetupLocation(npc.ParentModel, invulnerability, involve, rotReturnToInteraction, rotToHero, forceExitInteraction);
                if (waitForInvolvement) {
                    await task;
                }
            }
            
            result.Complete();
        }
    }
}