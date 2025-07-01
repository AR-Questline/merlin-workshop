using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.BuildUp;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class HasStatusUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            var characterInput = RequiredARValueInput<IAlive>("character");
            var statusTemplate = RequiredARValueInput<TemplateWrapper<StatusTemplate>>("statusTemplate");
            var hasStatus = ValueOutput<bool>("hasStatus");
            var hasActiveStatus = ValueOutput<bool>("hasActiveStatus");
            var statusOutput = ValueOutput<Status>("status");
            DefineSimpleAction(flow => {
                IAlive alive = characterInput.Value(flow);
                ICharacter character = alive as ICharacter;
                StatusTemplate template = statusTemplate.Value(flow).Template;
                if (character == null || template == null) {
                    flow.SetValue(hasStatus, false);
                    flow.SetValue(hasActiveStatus, false);
                    flow.SetValue(statusOutput, null);
                    return;
                }
                var status = character.Statuses.AllStatuses.FirstOrDefault(s => s.Template == template);
                bool hasThisStatus = status != null;
                flow.SetValue(hasStatus, hasThisStatus);
                flow.SetValue(hasActiveStatus, hasThisStatus && status is not BuildupStatus {Active: false});
                flow.SetValue(statusOutput, status);
            });
        }
    }
}