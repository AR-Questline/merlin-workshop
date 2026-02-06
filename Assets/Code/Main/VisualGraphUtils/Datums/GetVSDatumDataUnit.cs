using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.VisualGraphUtils.Datums {
    [UnitCategory("AR/Variables/Get VS Datum")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("Get VS Datum Data")]
    [UnityEngine.Scripting.Preserve]
    public class GetVSDatumDataUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public VSDatumType type;
        
        protected override void Definition() {
            var name = InlineARValueInput("name", "var");
            var datums = FallbackARValueInput("datums", flow => flow.stack.self);
            ValueOutput(typeof(VSDatumType), "type", _ => type);
            ValueOutput(typeof(VSDatumValue), "value", flow => datums.Value(flow).GetComponent<VSDatums>().GetDatum(name.Value(flow), type));
        }
    }
}