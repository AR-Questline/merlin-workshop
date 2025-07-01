using Awaken.TG.Main.Character;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class SkillResetFactionOverride : ARUnit, ISkillUnit {
        protected override void Definition() {
            var target = RequiredARValueInput<ICharacter>("target");
            DefineSimpleAction("Enter", "Exit", flow => {
                target.Value(flow).ResetFactionOverride();
            });
        }
    }
}