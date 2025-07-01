using Awaken.TG.Main.Heroes.Combat;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Getters {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetOwnedSummonsUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            ValueOutput("anySummonOwned", this.GetAnySummonsOwned);
            ValueOutput("skillItemOwnedSummonsCount", this.GetOwnedSummonsForSkillItemCount);
            ValueOutput("allOwnedSummonsCount", this.GetOwnedSummonsCount);
        }
    }
}