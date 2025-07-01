using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Enter Void")]
    public class SEditorEnterVoid : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SEnterVoid();
        }
    }

    public partial class SEnterVoid : StoryStep {
        public override StepResult Execute(Story story) {
            ModelUtils.GetSingletonModel<WyrdsphereVoid>();
            return StepResult.Immediate;
        }
    }
}