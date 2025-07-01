using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Duel/Duel: Manual End")]
    public class SEditorDuelManualEnd : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDuelManualEnd();
        }
    }

    public partial class SDuelManualEnd : StoryStep {
        public override StepResult Execute(Story story) {
            var duelController = World.Any<DuelController>();
            if (duelController == null) {
                Log.Minor?.Error("No duel in progress, so duel can't be finished");
                return StepResult.Immediate;
            }
            duelController.EndDuel();
            return StepResult.Immediate;
        }
    }
}