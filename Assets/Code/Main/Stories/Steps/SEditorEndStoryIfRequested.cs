using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Story: End Story if requested")]
    public class SEditorEndStoryIfRequested : EditorStep {
        public bool stopInvolvingHero = true;
        
        public override bool MayHaveContinuation => true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SEndStoryIfRequested {
                stopInvolvingHero = stopInvolvingHero
            };
        }
    }

    public partial class SEndStoryIfRequested : StoryStep {
        public bool stopInvolvingHero = true;
        
        public override StepResult Execute(Story story) {
            if (story is Story { HasBeenDiscarded: false, ManualInterruptRequested: true }) {
                StoryUtils.EndStory(story, stopInvolvingHero);
            }
            return StepResult.Immediate;
        }
    }
}