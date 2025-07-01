using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("UI/Story: Wait For Discard")]
    public class SEditorWaitForPopupDiscard : EditorStep {
        public StoryBookmark bookmark;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SWaitForPopupDiscard {
                bookmark = bookmark
            };
        }
    }

    public partial class SWaitForPopupDiscard : StoryStep {
        public StoryBookmark bookmark;
        
        public override StepResult Execute(Story story) {
            story.ListenTo(Model.Events.BeforeDiscarded, RunAfterDiscard, story);
            return StepResult.Immediate;
        }

        void RunAfterDiscard() {
            Story.StartStory(new StoryConfig(Hero.Current, null, bookmark, null));
        }
    }
}
