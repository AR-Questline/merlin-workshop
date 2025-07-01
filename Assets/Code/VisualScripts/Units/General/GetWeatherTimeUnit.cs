using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/Time")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetWeatherTimeUnit : Unit {
        protected override void Definition() {
            ValueOutput("time", flow => World.Only<GameRealTime>().WeatherTime);
        }
    }
}