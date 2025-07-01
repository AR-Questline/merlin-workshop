using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Duel/Duel: Manual Start")]
    public class SEditorDuelManualStart : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDuelManualStart();
        }
    }

    public partial class SDuelManualStart : StoryStep {
        public override StepResult Execute(Story story) {
            var duelController = World.Any<DuelController>();
            if (duelController == null) {
                Log.Minor?.Error("No duel in progress, so duel can't be started");
                return StepResult.Immediate;
            }
            duelController.StartDuel();
            return StepResult.Immediate;
        }
    }
}