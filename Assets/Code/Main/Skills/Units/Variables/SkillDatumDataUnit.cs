using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("Skill Datum Data")]
    [UnityEngine.Scripting.Preserve]
    public class SkillDatumDataUnit : ARUnit, ISkillUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public VSDatumType type;
        
        protected override void Definition() {
            var name = InlineARValueInput("name", "");
            ValueOutput(typeof(VSDatumType), "type", _ => type);
            ValueOutput(typeof(VSDatumValue), "value", flow => this.Skill(flow).GetDatum(name.Value(flow), type) ?? default);
        }
    }
}