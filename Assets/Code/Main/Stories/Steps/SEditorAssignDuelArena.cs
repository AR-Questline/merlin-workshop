using Awaken.TG.Main.Fights.Duels;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Runtime;
using Awaken.TG.Main.Stories.Runtime.Nodes;
using Awaken.TG.MVC;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Main.Stories.Steps {
    [Element("Game/Duel/Duel: Teleport To Arena"), NodeSupportsOdin]
    public class SEditorAssignDuelArena : EditorStep {
        public DuelArenaReferenceData duelArenaReferenceData;
        public bool teleportToArenaScene = true;
        public bool teleportToArena = true;
        public bool automaticallyActivateArena = true;

        protected override StoryStep CreateRuntimeStepImpl(StoryGraphParser parser) {
            return new SAssignDuelArena {
                duelArenaReferenceData = duelArenaReferenceData,
                teleportToArenaScene = teleportToArenaScene,
                teleportToArena = teleportToArena,
                automaticallyActivateArena = automaticallyActivateArena
            };
        }
    }

    public partial class SAssignDuelArena : StoryStep {
        public DuelArenaReferenceData duelArenaReferenceData;
        public bool teleportToArenaScene = true;
        public bool teleportToArena = true;
        public bool automaticallyActivateArena = true;

        public override StepResult Execute(Story story) {
            if (!duelArenaReferenceData.TryGetArenaData(story, out var data)) {
                return StepResult.Immediate;
            }

            var duelController = World.Any<DuelController>();
            if (duelController == null) {
                Log.Minor?.Error("No duel in progress, so can't enter arena");
                return StepResult.Immediate;
            }

            var result = new StepResult();
            Teleport(duelController, data, result).Forget();
            return result;
        }

        async UniTaskVoid Teleport(DuelController duelController, DuelArenaData data, StepResult result) {
            await duelController.AssignArena(data, teleportToArenaScene, teleportToArena, automaticallyActivateArena);
            result.Complete();
        }
    }

}