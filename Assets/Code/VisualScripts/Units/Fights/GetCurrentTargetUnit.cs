using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.VisualGraphUtils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetCurrentTargetUnit : ARUnit {
        protected override void Definition() {
            var character = FallbackARValueInput("character", flow => flow.stack.self);
            ValueOutput("target", flow => VGUtils.GetModel<NpcElement>(character.Value(flow)).GetCurrentTarget()?.CharacterView?.transform);
        }
    }
}
