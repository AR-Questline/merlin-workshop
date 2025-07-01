using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.AI.Graphs {
    [UnitCategory("AR/AI_Systems/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class GetTargetRange : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public RangeBetween between;
        
        protected override void Definition() {
            ValueOutput("range", flow => NpcAI(flow).GetTargetRange(between));
        }
    }
}