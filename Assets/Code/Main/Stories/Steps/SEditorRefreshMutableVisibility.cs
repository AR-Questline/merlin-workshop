using Awaken.TG.Main.Locations.Attachments.Elements;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Refresh Visibility")]
    public class SEditorRefreshMutableVisibility : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SRefreshMutableVisibility();
        }
    }

    public partial class SRefreshMutableVisibility : StoryStep {
        public override StepResult Execute(Story story) {
            foreach (var mutableVisibility in World.All<MutableVisibility>()) {
                mutableVisibility.Refresh();
            }
            return StepResult.Immediate;
        }
    }
}