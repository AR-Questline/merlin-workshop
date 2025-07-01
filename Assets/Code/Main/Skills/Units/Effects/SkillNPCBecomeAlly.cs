using Awaken.TG.Main.AI.SummonsAndAllies;
using Awaken.TG.Main.Character;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillNPCBecomeAlly : ARUnit, ISkillUnit {
        protected override void Definition() {
            var target = RequiredARValueInput<ICharacter>("target");
            var ally = FallbackARValueInput("allyCharacter",
                flow => this.Skill(flow).Owner);
            DefineSimpleAction("Enter", "Exit", flow => {
                var allyCharacter = ally.Value(flow);
                if (allyCharacter == null) {
                    return;
                }
                var targetCharacter = target.Value(flow);
                if (targetCharacter.HasElement<NpcAlly>()) {
                    var npcAlly = targetCharacter.TryGetElement<NpcAlly>();
                    if (npcAlly.Ally == allyCharacter) {
                        return;
                    } else {
                        npcAlly.Discard();
                    }
                }
                target.Value(flow).AddElement(new NpcAlly(allyCharacter));
            });
        }
    }
}