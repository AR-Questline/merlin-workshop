using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units {
    [UnityEngine.Scripting.Preserve]
    public class IsOfType : Unit {
        public ValueInput input;
        public ValueInput type;
        [UnityEngine.Scripting.Preserve] public ValueOutput output;
        
        protected override void Definition() {
            input = ValueInput<object>("Input");
            type = ValueInput("Type", typeof(Component));
            output = ValueOutput("Output", f => {
                object valueInput = f.GetValue<object>(input);
                if (valueInput == null) {
                    return false;
                }
                Type valueType = valueInput.GetType();
                Type targetType = f.GetValue<Type>(type);
                return valueType == targetType || targetType.IsAssignableFrom(valueType);
            });
        }
    }
}