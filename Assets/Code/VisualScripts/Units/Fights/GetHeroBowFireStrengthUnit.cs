using Awaken.TG.Main.Animations.FSM.Heroes.Machines;
using Awaken.TG.Main.Heroes;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Fights {
    [UnitCategory("AR/Fights")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetHeroBowFireStrengthUnit : ARUnit {
        protected override void Definition() {
            ValueOutput("BowFireStrength", _ => Hero.Current.TryGetElement<BowFSM>()?.FireStrength ?? 0f);
        }
    }
}