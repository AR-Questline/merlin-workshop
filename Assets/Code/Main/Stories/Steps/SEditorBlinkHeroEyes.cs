using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Hero: Blink Eyes"), NodeSupportsOdin]
    public class SEditorBlinkHeroEyes : EditorStep {
        [LabelWidth(130)] public float blinkingDuration = 0.5f;
        [LabelWidth(130)] public float closedEyesDuration = 0.5f;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SBlinkHeroEyes {
                blinkingDuration = blinkingDuration,
                closedEyesDuration = closedEyesDuration
            };
        }
    }

    public partial class SBlinkHeroEyes : StoryStep {
        public float blinkingDuration = 0.5f;
        public float closedEyesDuration = 0.5f;
        
        public override StepResult Execute(Story story) {
            var result = new StepResult();
            World.Services.Get<TransitionService>().TransitionToBlack(blinkingDuration).Forget();
            World.EventSystem.Trigger(Hero.Current, SCloseHeroEyes.Events.EyesClosed, true);
            WaitWithClosedEyes(story, result).Forget();
            return result;
        }

        async UniTaskVoid WaitWithClosedEyes(Story api, StepResult result) {
            await AsyncUtil.BlockInputUntilDelay(api, blinkingDuration, false, true);
            result.Complete();
            await AsyncUtil.BlockInputUntilDelay(api, closedEyesDuration, false, true);
            World.Services.Get<TransitionService>().TransitionFromBlack(blinkingDuration).Forget();
        }
    }
}