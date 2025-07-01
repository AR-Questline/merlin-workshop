using Awaken.TG.Main.General.Configs;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General/Variables")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetProjectileHitMaskUnit : ARUnit {
        protected override void Definition() {
            ValueOutput("hitMask", _ => GameConstants.Get.projectileHitMask);
        }
    }
}