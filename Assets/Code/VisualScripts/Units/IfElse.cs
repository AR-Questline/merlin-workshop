using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units {
    [UnitCategory("Control")]
    [TypeIcon(typeof(If))]
    [UnityEngine.Scripting.Preserve]
    public class IfElse : Unit {
        [Serialize, Inspectable, UnitHeaderInspectable]
        public int count;

        protected override void Definition() {
            var options = new ValueInput[count];
            var triggers = new ControlOutput[count];

            ControlOutput("never");
            for (int i = 0; i < count; i++) {
                options[i] = ValueInput<bool>($"in_{i}");
                triggers[i] = ControlOutput($"out_{i}");
            }
            var @else = ControlOutput("else");

            ControlInput("try", flow => {
                for (int i = 0; i < count; i++) {
                    if (flow.GetValue<bool>(options[i])) {
                        return triggers[i];
                    }
                }
                return @else;
            });
        }
    }
}