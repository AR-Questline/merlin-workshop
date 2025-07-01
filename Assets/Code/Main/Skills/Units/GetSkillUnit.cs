using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units {
    [UnitCategory("AR/Skills")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetSkillUnit : Unit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("skill", this.Skill);
        }
    }
}