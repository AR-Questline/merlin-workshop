using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class RemoveAllStatusOfType : ARUnit, ISkillUnit {
        protected override void Definition() {
            var statusType = RequiredARValueInput<StatusType>("statusType");
            var character = FallbackARValueInput("character", flow => this.Skill(flow).Owner);
            DefineSimpleAction(flow => {
                character.Value(flow).Statuses.RemoveAllStatusesOfType(statusType.Value(flow));
            });
        }
    }
}