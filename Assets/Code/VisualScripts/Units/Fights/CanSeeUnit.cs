using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class CanSeeUnit : ARUnit {
        protected override void Definition() {
            var inCharacter = FallbackARValueInput("character", VGUtils.My<ICharacter>);
            var inTarget = RequiredARValueInput<ICharacter>("target");
            ValueOutput("", flow => inCharacter.Value(flow).AIEntity.CanSee(inTarget.Value(flow).AIEntity));
        }
    }
}