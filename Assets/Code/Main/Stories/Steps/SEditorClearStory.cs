using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Story: Clear UI")]
    public class SEditorClearStory : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SClearStory();
        }
    }
    
    public partial class SClearStory : StoryStep {
        public override StepResult Execute(Story story) {
            story.Clear();
            return StepResult.Immediate;
        }
    }
}
