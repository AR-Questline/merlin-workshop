using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.Tutorials;
using Awaken.TG.Main.Tutorials.TutorialPopups;
using Awaken.TG.MVC;
using Sirenix.OdinInspector;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Technical/Tutorial: Show Text")]
    public class SEditorShowTutorialText : EditorStep {
        [LabelText("Tutorial Title"), LocStringCategory(Category.UI)]
        public LocString title;
        [LabelText("Tutorial Text"), LocStringCategory(Category.UI)]
        public LocString text;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SShowTutorialText {
                title = this.title,
                text = this.text
            };
        }
    }
    
    public partial class SShowTutorialText : StoryStep {
        public LocString title;
        public LocString text;
        
        public sealed override StepResult Execute(Story story) {
            if (World.Only<ShowTutorials>().Enabled) {
                return GetStepResult(story);
            }
            return StepResult.Immediate;
        }
        
        protected virtual Model Show() => TutorialText.Show(new TutorialConfig.TextTutorial {
            title = title, 
            text = text
        });

        StepResult GetStepResult(Story api) {
            StepResult result = new();
            var model = Show();
            model.ListenTo(Model.Events.BeforeDiscarded, () => result.Complete(), api);
            return result;
        }
    }
}