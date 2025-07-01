using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.VisualGraphUtils.Datums {
    [UnitCategory("AR/Variables/Get VS Datum")]
    [TypeIcon(typeof(FlowGraph))]
    [UnitTitle("Get VS Datum")]
    [UnityEngine.Scripting.Preserve]
    public class GetVSDatumUnit : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public VSDatumType type;
        
        protected override void Definition() {
            var name = InlineARValueInput("name", "var");
            var datums = FallbackARValueInput("datums", flow => flow.stack.self);
            ValueOutput(type.GetType(), "value", flow => {
                var value = datums.Value(flow).GetComponent<VSDatums>().GetDatum(name.Value(flow), type);
                return type.GetValue(value);
            });
        }
    }
}