using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class RemoveStatus : ARUnit, ISkillUnit {
        protected override void Definition() {
            var status = RequiredARValueInput<Status>("status");
            var character = FallbackARValueInput("character", flow => this.Skill(flow).Owner);
            DefineSimpleAction(flow => {
                var characterValue = character.Value(flow);
                var statusValue = status.Value(flow);
                if (characterValue is not { HasBeenDiscarded: false } || statusValue is not { HasBeenDiscarded: false }) {
                    return;
                }
                characterValue.Statuses.RemoveStatus(statusValue);
            });
        }
    }
}