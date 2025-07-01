using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Getters {
    [UnitCategory("AR/Skills/Getters")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetStatusBuildup : ARUnit {
        protected override void Definition() {
            var status = RequiredARValueInput<Status>("status");
            var hasBuildup = ValueOutput<bool>("hasBuildup");
            var buildup = ValueOutput<float>("buildup");
            
            DefineSimpleAction(flow => {
                Status statusValue = status.Value(flow);
                bool hasBuildupValue = statusValue is BuildupStatus;
                float buildupValue = hasBuildupValue ? ((BuildupStatus) statusValue).BuildupProgress : 1;
                flow.SetValue(hasBuildup, hasBuildupValue);
                flow.SetValue(buildup, buildupValue);
            });
        }
    }
}