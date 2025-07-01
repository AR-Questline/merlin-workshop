using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Hero: Crouch")]
    public class SEditorPlayerCrouch : EditorStep {
        public bool targetState;
        
        [ShowIf(nameof(targetState))]
        [Tooltip("If checked, player will stand up after this story ends.")]
        public bool revertOnStoryEnd = true;
        
        public float duration = 0.5f;
        [Tooltip("If true, the real crouch will be used, it will alert all NPCs about hero sneaking etc., use it only if it's required or hero will be left in this state.")]
        public bool useRealCrouch;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SPlayerCrouch {
                targetState = targetState,
                revertOnStoryEnd = revertOnStoryEnd,
                duration = duration,
                useRealCrouch = useRealCrouch
            };
        }
    }

    public partial class SPlayerCrouch : StoryStep {
        public bool targetState;
        public bool revertOnStoryEnd = true;
        public bool useRealCrouch;
        public float duration = 0.5f;
        
        public override StepResult Execute(Story story) {
            if (targetState) {
                HeroStoryCrouch.StartCrouching(story, duration, useRealCrouch, revertOnStoryEnd);
            } else {
                HeroStoryCrouch.StopCrouching(story, duration);
            }
            
            return StepResult.Immediate;
        }
    }
}