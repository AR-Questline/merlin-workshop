using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Heroes.Statuses.Duration;
using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ApplyStatus : ARUnit, ISkillUnit {
        protected override void Definition() {
            var statusTemplate = RequiredARValueInput<TemplateWrapper<StatusTemplate>>("statusTemplate");
            var character = FallbackARValueInput<IAlive>("character", flow => this.Skill(flow)?.Owner);
            var sourceCharacter = OptionalARValueInput<ICharacter>("sourceCharacter");
            var duration = FallbackARValueInput<float>("duration", _ => -1);
            var overrides = FallbackARValueInput<SkillVariablesOverride>("variables", _ => null);
            var buildupStrengthIfPossible = InlineARValueInput<float>("buildupStrengthIfPossible", 0);
            var type = ValueOutput<StatusAddType>("addType");
            var oldStatus = ValueOutput<Status>("oldStatus");
            var newStatus = ValueOutput<Status>("newStatus");
            
            DefineSimpleAction(flow => {
                StatusTemplate template = statusTemplate.Value(flow).Template;
                IAlive targetAlive = character.Value(flow);
                
                if (targetAlive is not ICharacter targetCharacter) {
                    return;
                }
                if (template == null || targetCharacter.HasBeenDiscarded) {
                    return;
                }
                CharacterStatuses characterStatuses = targetCharacter.Statuses;

                Skill skill = this.Skill(flow);
                StatusSourceInfo statusSourceInfo = skill != null 
                    ? StatusSourceInfo.FromSkill(skill, template) 
                    : StatusSourceInfo.FromStatus(template);
                
                ICharacter sourceChar = sourceCharacter.HasValue ? sourceCharacter.Value(flow) : null;
                if (sourceChar != null) {
                    statusSourceInfo.WithCharacter(sourceChar);
                }
                
                CharacterStatuses.AddResult result = VGUtils.ApplyStatus(characterStatuses, template, statusSourceInfo,
                    buildupStrengthIfPossible.Value(flow), duration.Value(flow), overrides.Value(flow));
                
                flow.SetValue(type, result.type);
                flow.SetValue(oldStatus, result.oldStatus);
                flow.SetValue(newStatus, result.newStatus);
            });
        }
    }
}