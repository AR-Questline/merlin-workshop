using Awaken.TG.Main.Heroes.Stats;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Generated.Stats {
    [UnitCategory("AR/Stats")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class IncreaseStatBy : ARUnit {
        protected override void Definition() {
            var stat = RequiredARValueInput<Stat>("stat");
            var value = InlineARValueInput("value", 0F);
            DefineSimpleAction(flow => stat.Value(flow).IncreaseBy(value.Value(flow)));
        }
    }
}