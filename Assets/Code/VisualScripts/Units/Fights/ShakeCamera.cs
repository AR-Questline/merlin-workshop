using Awaken.TG.Main.Cameras;
using Awaken.TG.MVC;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/AI_Systems/Combat")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ShakeCamera : ARUnit {
        protected override void Definition() {
            var amplitude = InlineARValueInput("amplitude", 0.5f);
            var frequency = InlineARValueInput("frequency", 0.15f);
            var time = InlineARValueInput("time", 0.5f);
            var pick = InlineARValueInput("pick", 0.1f);
            DefineNoNameAction(flow =>
                World.Only<GameCamera>().Shake(false, amplitude.Value(flow), frequency.Value(flow), time.Value(flow), pick.Value(flow)).Forget());
        }
    }
}