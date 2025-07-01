using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Hero/Dialogue/Hero: Stop Involving")]
    public class SEditorStopInvolvingHero : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStopInvolvingHero();
        }
    }

    public partial class SStopInvolvingHero : StoryStep {
        public override StepResult Execute(Story story) {
            story.RemoveElementsOfType<HeroDialogueInvolvement>();
            story.View<VDialogue>()?.ShowOnlyText();
            return StepResult.Immediate;
        }
    }
}