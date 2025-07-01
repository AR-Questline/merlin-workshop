using Awaken.TG.Main.Heroes.Interactions;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Show Fake Interaction Prompt"), NodeSupportsOdin]
    public class SEditorFakeInteractionPrompt : EditorStep {
        public LocString title;
        public LocString description;
        
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SFakeInteractionPrompt {
                title = title,
                description = description
            };
        }
    }
    
    public partial class SFakeInteractionPrompt : StoryStep {
        public LocString title;
        public LocString description;
        
        public override StepResult Execute(Story story) {
            var stepResult = new StepResult();
            story.AddElement(new StoryFakeHeroInteractionUI(title, description, stepResult));
            return stepResult;
        }
    }
}