using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ModifyLockDifficultyUnit : ARUnit {
        protected override void Definition() {
            var lockAction = RequiredARValueInput<LockAction>("lockAction");
            var difficulty = InlineARValueInput("difficultyDecrease", 1);
            DefineSimpleAction(flow => lockAction.Value(flow).DecreaseDifficulty(difficulty.Value(flow)));
        }
    }
}