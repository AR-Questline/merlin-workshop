using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.Factions;
using Awaken.TG.VisualScripts.Units.Typing;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetFactionTemplateUnit : ARUnit {
        protected override void Definition() {
            var character = RequiredARValueInput<ICharacter>("character");
            ValueOutput("FactionTemplate", flow => new TemplateWrapper<FactionTemplate>(character.Value(flow).Faction.Template));
        }
    }
}