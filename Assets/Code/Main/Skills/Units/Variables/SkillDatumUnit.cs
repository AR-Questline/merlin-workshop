using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("Skill Datum")]
    [UnityEngine.Scripting.Preserve]
    public class SkillDatumUnit : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public VSDatumType type;
        
        protected override void Definition() {
            var name = InlineARValueInput("name", "");
            ValueOutput(type.GetType(), "value", flow => {
                var value = this.Skill(flow).GetDatum(name.Value(flow), type) ?? default;
                return type.GetValue(value);
            });
        }
    }
}