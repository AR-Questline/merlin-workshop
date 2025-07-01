using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.Main.UI.RoguePreloader;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Game: Exit to Menu")]
    public class SEditorExitToMenu : EditorStep {

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SExitToMenu();
        }
    }

    public partial class SExitToMenu : StoryStep {
        
        public override StepResult Execute(Story story) {
            story.FinishStory();
            ScenePreloader.LoadTitleScreen();
            return StepResult.Immediate;
        }
    }
}