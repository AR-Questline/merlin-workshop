using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.HUD;
using Awaken.TG.MVC;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.UI {
    [UnitCategory("AR/UI")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ToggleRedVignetteUnit : ARUnit {
        protected override void Definition() {
            var activeRef = InlineARValueInput("active", false);
            DefineSimpleAction(flow => {
                bool active = activeRef.Value(flow);
                Hero.Current?.Trigger(VCHeroHealthVignette.Events.HealthVignetteToggled, active);
            });
        }
    }
}