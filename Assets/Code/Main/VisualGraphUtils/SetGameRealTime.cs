using Awaken.TG.Main.Timing;
using Awaken.TG.MVC;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.VisualGraphUtils {
    [UnitCategory("AR/Time")]
    [UnityEngine.Scripting.Preserve]
    public class SetGameRealTime : ARUnit {
        protected override void Definition() {
            var hours = InlineARValueInput("hour", 0);
            var minutes = InlineARValueInput("minute", 0);
            DefineSimpleAction(flow => {
                var h = hours.Value(flow);
                var m = minutes.Value(flow);
                var gameRealTime = World.Only<GameRealTime>();
                gameRealTime.SetWeatherTime(h, m);
            });
        }
    }
}