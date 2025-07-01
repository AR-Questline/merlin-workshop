using Unity.VisualScripting;
using UnityEngine;

namespace Awaken.TG.VisualScripts.Units {
    
    [UnitCategory("Nulls")]
    [TypeIcon(typeof(Null))]
    public class NullCheckWithExit : Unit {

        [DoNotSerialize] [PortLabelHidden]
        public ControlInput enter;

        [DoNotSerialize] [PortLabelHidden]
        public ValueInput input;

        [DoNotSerialize] [PortLabel("Exit")]
        public ControlOutput exit;

        [DoNotSerialize] [PortLabel("Not Null")]
        public ControlOutput ifNotNull;

        [DoNotSerialize] [PortLabel("Null")]
        public ControlOutput ifNull;
            
        protected override void Definition() {
            enter = ControlInput(nameof(enter), Enter);
            input = ValueInput<object>(nameof(input)).AllowsNull();
            exit = ControlOutput(nameof(exit));
            ifNotNull = ControlOutput(nameof(ifNotNull));
            ifNull = ControlOutput(nameof(ifNull));

            Requirement(input, enter);
            Succession(enter, ifNotNull);
            Succession(enter, ifNull);
            Succession(enter, exit);
        }
        
        ControlOutput Enter(Flow flow) {
            var input = flow.GetValue(this.input);

            bool isNull;

            if (input is Object) {
                // Required cast because of Unity's custom == operator.
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                isNull = (Object)input == null;
            } else {
                isNull = input == null;
            }

            flow.Invoke(isNull ? ifNull : ifNotNull);

            return exit;
        }
    }
}