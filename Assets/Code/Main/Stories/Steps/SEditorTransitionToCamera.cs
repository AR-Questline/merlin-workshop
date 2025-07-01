using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Transition: To Camera")]
    public class SEditorTransitionToCamera : EditorStep {
        public float duration;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STransitionToCamera {
                duration = duration
            };
        }
    }
    
    public partial class STransitionToCamera : StoryStep {
        public float duration;
        
        public override StepResult Execute(Story story) {
            var result = new StepResult();
            ToCamera(result).Forget();
            return result;
        }

        async UniTaskVoid ToCamera(StepResult result) {
            await World.Services.Get<TransitionService>().ToCamera(duration);
            result.Complete();
        }
    }
}