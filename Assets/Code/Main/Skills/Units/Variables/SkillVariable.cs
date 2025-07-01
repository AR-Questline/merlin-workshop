using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    public class SkillVariable : Unit, ISkillUnit {

        [Serialize, Inspectable, UnitHeaderInspectable]
        public string name;

        [UnityEngine.Scripting.Preserve] ValueOutput _value;
        
        protected override void Definition() {
            _value = ValueOutput("value", flow => this.Skill(flow).GetVariable(name, null) ?? 0);
        }
    }
}