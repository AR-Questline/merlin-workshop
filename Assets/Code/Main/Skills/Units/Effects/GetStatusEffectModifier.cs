using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetStatusEffectModifier : ARUnit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("modifier", flow => (this.Skill(flow).ParentModel as Status)?.EffectModifier ?? 1f);
        }
    }
}