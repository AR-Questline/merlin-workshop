using Awaken.TG.Graphics.Transitions;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Hero: Open Eyes")]
    public class SEditorOpenHeroEyes : EditorStep {
        public float duration = 3;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SOpenHeroEyes {
                duration = duration
            };
        }
    }
    
    public partial class SOpenHeroEyes : StoryStep {
        public float duration = 3;
        
        public override StepResult Execute(Story story) {
            var result = new StepResult();
            OpenHeroEyes(result).Forget();
            return result;
        }
        
        async UniTaskVoid OpenHeroEyes(StepResult result) {
            if (await World.Services.Get<TransitionService>().TransitionFromBlack(duration)) {
                result.Complete();
            }
        }
    }
}