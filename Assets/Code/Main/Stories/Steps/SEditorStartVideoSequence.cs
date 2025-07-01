using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Utility.Video;
using Awaken.TG.MVC;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Cutscenes/Cutscene: Start Sequence"), NodeSupportsOdin]
    public class SEditorStartVideoSequence : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStartVideoSequence();
        }
    }

    public partial class SStartVideoSequence : StoryStep {
        public override StepResult Execute(Story story) {
            if (DebugReferences.FastStory || DebugReferences.ImmediateStory) {
                return StepResult.Immediate;
            }

            World.Add(new VideoBlackBackground(story));
            return StepResult.Immediate;
        }
    }
}