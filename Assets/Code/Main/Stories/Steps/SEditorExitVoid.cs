using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Exit Void")]
    public class SEditorExitVoid : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SExitVoid();
        }
    }

    public partial class SExitVoid : StoryStep {
        public override StepResult Execute(Story story) {
            ModelUtils.RemoveSingletonModel<WyrdsphereVoid>();
            return StepResult.Immediate;
        }
    }
}