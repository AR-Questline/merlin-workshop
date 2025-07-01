using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Awaken.Utility.Debugging;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class RemoveAllStatusOfKind: ARUnit, ISkillUnit {
        protected override void Definition() {
            var statusTemplate = RequiredARValueInput<TemplateWrapper<StatusTemplate>>("statusTemplate");
            var character = FallbackARValueInput("character", flow => this.Skill(flow).Owner);
            DefineSimpleAction(flow => {
                if (character.Value(flow) is { HasBeenDiscarded: false } characterValue) {
                    characterValue.Statuses.RemoveAllStatus(statusTemplate.Value(flow).Template);
                } else {
                    Log.Minor?.Error($"{flow.stack.machine} - Character {character.Value(flow)} has been discarded, cannot remove status {statusTemplate.Value(flow)?.Template.name}");
                }
            });
        }
    }
}