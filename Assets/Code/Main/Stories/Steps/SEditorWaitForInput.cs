using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Delay: Wait for Input")]
    public class SEditorWaitForInput : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SWaitForInput();
        }
    }

    public partial class SWaitForInput : StoryStep {
        public override StepResult Execute(Story story) {
            return StoryUtils.WaitForInput(story);
        }
    }
}