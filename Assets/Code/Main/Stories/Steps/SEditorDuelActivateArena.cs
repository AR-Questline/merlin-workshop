using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Duel/Duel: Activate Arena"), NodeSupportsOdin]
    public class SEditorDuelActivateArena : EditorStep {
        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SDuelActivateArena();
        }
    }

    public partial class SDuelActivateArena : StoryStep {
        public override StepResult Execute(Story story) {
            var duelController = World.Any<DuelController>();
            if (duelController == null) {
                Log.Minor?.Error("No duel in progress, so can't enter arena");
                return StepResult.Immediate;
            }

            if (!duelController.TryActivateArena()) {
                Log.Minor?.Error($"Failed to activate Duel Arena. {story}");
            }
            return StepResult.Immediate;
        }
    }
}