using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units.Custom.NotToFrequents {
    [UnitCategory("Custom/NotToFrequent")]
    [TypeIcon(typeof(Time))]
    [UnityEngine.Scripting.Preserve]
    public class TryNotTooFrequent : Unit {
        protected override void Definition() {
            var priorityInput = ValueInput("priority", 0);
            var dataInput = ValueInput<NotTooFrequent>("data");
            var output = ControlOutput("");
            var input = ControlInput("", flow => {
                var priority = flow.GetValue<int>(priorityInput);
                var data = flow.GetValue<NotTooFrequent>(dataInput);
                if (data.Try(priority)) {
                    return output;
                } else {
                    return null;
                }
            });
            Requirement(dataInput, input);
            Succession(input, output);
        }
    }
}