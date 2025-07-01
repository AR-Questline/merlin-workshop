using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units {
    [UnitCategory("AR/Skills")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class IsSkillItemUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var item = RequiredARValueInput<Item>("Item To Check");
            var success = ControlOutput("Success");
            var fail = ControlOutput("Fail");

            var checkItem = ControlInput("Check Item", flow => {
                bool validItem = GetSkillItemUnit.GetSkillItem(flow, this) == item.Value(flow);
                return validItem ? success : fail;
            });
            
            Succession(checkItem, success);
            Succession(checkItem, fail);
        }
    }
}