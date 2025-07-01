using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units {
    public class Cast : Unit {
        public ValueInput input;
        public ValueInput type;
        public ValueOutput output;
        
        protected override void Definition() {
            input = ValueInput<object>("Input");
            type = ValueInput("Type", typeof(Component));
            output = ValueOutput("Output", f => f.GetValue<object>(input));
        }
    }
}