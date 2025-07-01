using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Stories.Steps.Helpers;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Story: End")]
    public class SEditorEndStory : EditorStep {
        public bool stopInvolvingHero = true;

        public override bool MayHaveContinuation => true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SEndStory {
                stopInvolvingHero = stopInvolvingHero
            };
        }
    }

    public partial class SEndStory : StoryStep {
        public bool stopInvolvingHero = true;
        
        public override StepResult Execute(Story story) {
            StoryUtils.EndStory(story, stopInvolvingHero);
            return StepResult.Immediate;
        }
    }
}