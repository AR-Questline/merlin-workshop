using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillNPCStopBeingAlly : ARUnit, ISkillUnit {
        protected override void Definition() {
            var target = RequiredARValueInput<ICharacter>("target");
            var ally = FallbackARValueInput("allyCharacter",
                flow => this.Skill(flow).Owner);
            DefineSimpleAction("Enter", "Exit", flow => {
                var allyCharacter = ally.Value(flow);
                if (allyCharacter == null) {
                    return;
                }
                target.Value(flow).Elements<NpcAlly>().FirstOrDefault(a => a.Ally == allyCharacter)?.Discard();
            });
        }
    }
}