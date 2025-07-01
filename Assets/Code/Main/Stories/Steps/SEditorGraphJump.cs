using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Branch/Branch: Graph Jump")]
    public class SEditorGraphJump : EditorStep {
        public StoryBookmark bookmark;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SGraphJump {
                bookmark = bookmark
            };
        }
    }

    public partial class SGraphJump : StoryStep {
        public StoryBookmark bookmark;
        
        public override StepResult Execute(Story story) {
            if (IsLastStep()) {
                bookmark.JumpToBookmark(story);
                return StepResult.Immediate;
            }
            
            var config = StoryConfig.Location(story.OwnerLocation, bookmark, story.MainView?.GetType());
            
            config.parentStory = story;
            foreach (var location in story.Locations) {
                config = config.WithLocation(location);
            }
            var newStory = Story.StartStory(config);
            var result = new StepResult();
            newStory.ListenTo(Model.Events.BeforeDiscarded, _ => result.Complete(), story);
            return result;
        }
    }
}