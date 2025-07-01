using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Location/Location: Start Story"), NodeSupportsOdin]
    public class SEditorLocationStartStory : EditorStep {
        public LocationReference locationReference;
        public StoryBookmark bookmark;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SLocationStartStory {
                locationReference = locationReference,
                bookmark = bookmark
            };
        }
    }
    
    public partial class SLocationStartStory : StoryStep {
        public LocationReference locationReference;
        public StoryBookmark bookmark;
        
        public override StepResult Execute(Story story) {
            var matchingLocations = locationReference.MatchingLocations(story);
            foreach (var location in matchingLocations) {
                StoryConfig storyConfig = StoryConfig.Location(location, bookmark, typeof(VDialogue));
                Story.StartStory(storyConfig);
            }
            return StepResult.Immediate;
        }
    }
}