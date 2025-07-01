using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units {
    [UnitCategory("Control")]
    [TypeIcon(typeof(If))]
    [UnityEngine.Scripting.Preserve]
    public class IfRandom : ARUnit {
        protected override void Definition() {
            var chanceInput = InlineARValueInput("chance", 100f);
            var trueOutput = ControlOutput("true");
            var falseOutput = ControlOutput("false");
            
            var input = ControlInput("", flow => {
                float chance = chanceInput.Value(flow);
                bool success = Random.value * 100f < chance;
                return success ? trueOutput : falseOutput;
            });
            
            Succession(input, trueOutput);
            Succession(input, falseOutput);
        }
    }
}