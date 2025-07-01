using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units {
    [UnitCategory("AR/Skills")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetSkillOwnerUnit : Unit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("owner", flow => {
                Skill skill = this.Skill(flow);
                return skill.WasDiscarded ? null : skill.Owner;
            });
        }
    }
}