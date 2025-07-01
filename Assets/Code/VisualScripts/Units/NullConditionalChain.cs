using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units {
    [UnitCategory("Nulls")]
    [TypeIcon(typeof(Null))]
    [UnityEngine.Scripting.Preserve]
    public class NullConditionalChain : Unit {
        [Serialize, Inspectable, UnitHeaderInspectable] public int tries;
        
        protected override void Definition() {
            var inputs = new ValueInput[tries];
            for (int i = 0; i < tries; i++) {
                inputs[i] = ValueInput<object>(i.ToString());
            }
            ValueOutput("", flow => {
                object output = null;
                for (int i = 0; i < tries; i++) {
                    output = flow.GetValue(inputs[i]);
                    if (output == null) {
                        return null;
                    }
                }
                return output;
            });
        }
    }
}