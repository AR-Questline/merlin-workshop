using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units {
    [UnitCategory("AR/Skills")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetMyStatus : ARUnit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("status", flow => this.Skill(flow).ParentModel as Status);
        }
    }
}