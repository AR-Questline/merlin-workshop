using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Tutorials;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Tutorial: Show")]
    public class SEditorTutorialShow : EditorStep {
        public TutKeys tutorial;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new STutorialShow {
                tutorial = tutorial
            };
        }
    }

    public partial class STutorialShow : StoryStep {
        public TutKeys tutorial;
        
        public override StepResult Execute(Story story) {
            TutorialMaster.Trigger(tutorial);
            return StepResult.Immediate;
        }
    }
}