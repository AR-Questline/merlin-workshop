using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Delay: Wait")]
    public class SEditorWaitForDelay : EditorStep {
        [LabelText("Duration[s]")]
        public float duration = 1f;
        [ShowIf(nameof(consumeInput)), Tooltip("If checked, player's action (click) will stop the delay and continue the story.")]
        public bool cancelable = true;
        [Tooltip("Check this if the player shouldn't be able to take any action during this delay.")]
        public bool consumeInput = true;
        [Tooltip("Check this if the time scale shouldn't change the duration. Useful if game is paused by other steps.")]
        public bool useUnscaledTime = false;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SWaitForDelay {
                duration = duration,
                cancelable = cancelable,
                consumeInput = consumeInput,
                useUnscaledTime = useUnscaledTime
            };
        }
    }

    public partial class SWaitForDelay : StoryStep {
        public float duration;
        public bool cancelable;
        public bool consumeInput;
        public bool useUnscaledTime;
        
        public override StepResult Execute(Story story) {
            var result = new StepResult();
            WaitForDelay(story, result).Forget();
            return result;
        }

        async UniTaskVoid WaitForDelay(Story api, StepResult result) {
            if (consumeInput) {
                await AsyncUtil.BlockInputUntilDelay(api, duration, cancelable, useUnscaledTime);
            } else {
                await AsyncUtil.DelayTime(api, duration, useUnscaledTime);
            }
            result.Complete();
        }
    }
}