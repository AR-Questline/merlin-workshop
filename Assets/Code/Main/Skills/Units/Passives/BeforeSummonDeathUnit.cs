using Awaken.TG.Main.AI.SummonsAndAllies;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class BeforeSummonDeathUnit : BeforeAllyDeathUnit {
        protected override void Trigger(GraphReference reference, NpcAlly payload) {
            if (payload is NpcHeroSummon) {
                base.Trigger(reference, payload);
            }
        }
    }
}