using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Dialogue/Hero: Involve")]
    public class SEditorInvolveHero : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SInvolveHero();
        }
    }

    public partial class SInvolveHero : StoryStep {
        public override StepResult Execute(Story story) {
            story.AddElement(new HeroDialogueInvolvement());
            return StepResult.Immediate;
        }
    }
}