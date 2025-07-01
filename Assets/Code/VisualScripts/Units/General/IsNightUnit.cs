using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class IsNightUnit : ARUnit {
        protected override void Definition() {
            ValueOutput("IsNight", _ => World.Only<GameRealTime>().WeatherTime.IsNight);
        }
    }
}