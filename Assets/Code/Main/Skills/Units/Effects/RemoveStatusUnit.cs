using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class RemoveStatusUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var characterInput = RequiredARValueInput<ICharacter>("character");
            var statusTemplate = RequiredARValueInput<TemplateWrapper<StatusTemplate>>("statusTemplate");
            DefineSimpleAction(flow => {
                ICharacter character = characterInput.Value(flow);
                StatusTemplate template = statusTemplate.Value(flow).Template;
                character.Statuses.RemoveStatus(template);
            });
        }
    }
}