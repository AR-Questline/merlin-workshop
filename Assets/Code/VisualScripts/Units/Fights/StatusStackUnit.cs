using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Skills;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/Skills/Status")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("Get Stack Level Unit")]
    [UnitSurtitle("Status")]
    [UnityEngine.Scripting.Preserve]
    public class StatusStackUnit : Unit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("stack", flow => Get(this, flow));
        }

        public static int Get(ISkillUnit unit, Flow flow) {
            Status status = unit.Skill(flow).ParentModel as Status;
            return status?.StackLevel ?? 1;
        }
    }
}