using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Variables {
    [UnitCategory("AR/Skills/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    public class StatTypeVariable : Unit, ISkillUnit {

        [Serialize, Inspectable, UnitHeaderInspectable]
        public string name;

        [UnityEngine.Scripting.Preserve] ValueOutput _statType;
        
        protected override void Definition() {
            _statType = ValueOutput("statType", flow => this.Skill(flow).GetRichEnum(name));
        }
    }
}