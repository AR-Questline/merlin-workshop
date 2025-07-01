using Awaken.TG.Main.Locations.Actions;
using Awaken.TG.Main.Locations.Actions.Lockpicking;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Getters {
    [UnitCategory("AR/Skills/Getters")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetLockDifficulty : ARUnit {
        protected override void Definition() {
            var lockAction = RequiredARValueInput<LockAction>("lockAction");
            var output = ValueOutput<LockTolerance>("difficulty");
            var intOutput = ValueOutput<int>("difficultyIndex");
            DefineSimpleAction(flow => {
                flow.SetValue(output, lockAction.Value(flow).Tolerance);
                flow.SetValue(intOutput, lockAction.Value(flow).Tolerance.index);
            });
        }
    }
}