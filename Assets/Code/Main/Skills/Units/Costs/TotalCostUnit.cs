using System.Linq;
using Awaken.TG.Main.General.Costs;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Costs {
    [TypeIcon(typeof(ICost))]
    [UnitCategory("AR/Skills/Costs")]
    [UnityEngine.Scripting.Preserve]
    public class TotalCostUnit : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable] int _count;
        protected override void Definition() {
            var costs = new RequiredValueInput<ICost>[_count];
            for (int i = 0; i < _count; i++) {
                costs[i] = RequiredARValueInput<ICost>(i.ToString());
            }
            ValueOutput("cost", flow => new TotalCost(costs.Select(c => c.Value(flow))));
        }
    }
}