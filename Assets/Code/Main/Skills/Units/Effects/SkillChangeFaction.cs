using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.VisualScripts.Units;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillChangeFaction : ARUnit, ISkillUnit {
        protected override void Definition() {
            var target = RequiredARValueInput<ICharacter>("target");
            var factionTemplate = FallbackARValueInput("factionTemplate",
                flow => new TemplateWrapper<FactionTemplate>(this.Skill(flow).Owner.Faction.Template));
            DefineSimpleAction("Enter", "Exit", flow => {
                var template = factionTemplate.Value(flow);
                if (template == null) {
                    return;
                }
                target.Value(flow).OverrideFaction(template.Template);
            });
        }
    }
}