using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/UI: Discard")]
    public class SEditorDiscardUI : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDiscardUI();
        }
    }
    
    public partial class SDiscardUI : StoryStep {
        public override StepResult Execute(Story story) {
            story.RemoveView();
            return StepResult.Immediate;
        }
    }
}