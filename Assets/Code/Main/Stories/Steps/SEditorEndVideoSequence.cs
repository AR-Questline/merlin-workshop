using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Cutscenes/Cutscene: End Sequence"), NodeSupportsOdin]
    public class SEditorEndVideoSequence : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SEndVideoSequence();
        }
    }

    public partial class SEndVideoSequence : StoryStep {
        public override StepResult Execute(Story story) {
            var bg = World.Any<VideoBlackBackground>();
            bg?.Discard();
            return StepResult.Immediate;
        }
    }
}