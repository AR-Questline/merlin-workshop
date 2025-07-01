using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;
using UnityEngine.AI;

namespace Awaken.TG.Main.AI.Movement.Units {
    [UnitCategory("AR/AI_Systems/Movement")]
    [UnityEngine.Scripting.Preserve]
    public class EnterStaggerUnit : ARUnit {
        protected override void Definition() {
            var npc = FallbackARValueInput("npc", NpcElement);
            var duration = FallbackARValueInput<float?>("duration", _ => null);
            DefineSimpleAction("Enter", "Exit", f => {
                npc.Value(f).EnterStaggerState(duration.Value(f));
            });
        }
    }
}