using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Game: New Game Plus")]
    public class SEditorStartNewGamePlus : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SStartNewGamePlus();
        }
    }

    public partial class SStartNewGamePlus : StoryStep {
        public override StepResult Execute(Story story) {
            story.FinishStory();
            NewGamePlusUtils.StartNewGamePlusDuringGameplay();
            return StepResult.Immediate;
        }
    }
}