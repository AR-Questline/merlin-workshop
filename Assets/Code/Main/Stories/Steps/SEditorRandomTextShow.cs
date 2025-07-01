using System.Linq;
using Awaken.TG.Code.Utility;
using Awaken.TG.Main.Fights.Utils;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Cysharp.Threading.Tasks;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Random: Text Show")]
    public class SEditorRandomTextShow : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SRandomTextShow();
        }
    }
    
    public partial class SRandomTextShow : StoryStep {
        public override StepResult Execute(Story story) {
            var sText = RandomUtil.UniformSelectSafe(parentChapter.steps.OfType<SText>().ToList());
            var result = new StepResult();
            AsyncExecute(sText, story, result).Forget();
            return result;
        }

        async UniTaskVoid AsyncExecute(SText stepToExecute, Story story, StepResult result) {
            StepResult waitForResult = stepToExecute.Execute(story);
            if (!await AsyncUtil.WaitUntil(story, () => waitForResult.IsDone)) {
                return;
            }
            if (parentChapter.continuation != null) {
                story.JumpTo(parentChapter.continuation);
            } else {
                story.FinishStory();
            }
            result.Complete();
        }
    }
}