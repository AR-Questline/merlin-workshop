using System.Collections;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units {
    
    /// <summary>
    /// Branches flow by checking if a condition is true or false.
    /// </summary>
    [UnitCategory("Control")]
    [UnitOrder(1)]
    [TypeIcon(typeof(If))]
    public class IfWithExit : Unit {
        /// <summary>
        /// The entry point for the branch.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        public ControlInput enter { get; private set; }

        /// <summary>
        /// The condition to check.
        /// </summary>
        [DoNotSerialize]
        [PortLabelHidden]
        public ValueInput condition { get; private set; }

        /// <summary>
        /// The action to execute if the condition is true
        /// </summary>
        [DoNotSerialize]
        [PortLabel("True")]
        public ControlOutput ifTrue { get; private set; }

        /// <summary>
        /// The action to execute if the condition is false
        /// </summary>
        [DoNotSerialize]
        [PortLabel("False")]
        public ControlOutput ifFalse { get; private set; }
        
        /// <summary>
        /// The action to execute after branching
        /// </summary>
        [DoNotSerialize]
        [PortLabel("Exit")]
        public ControlOutput exit { get; private set; }
        
        
        protected override void Definition() {
            enter = ControlInputCoroutine(nameof(enter), Enter, EnterCoroutine);
            condition = ValueInput<bool>(nameof(condition));
            exit = ControlOutput(nameof(exit));
            ifTrue = ControlOutput(nameof(ifTrue));
            ifFalse = ControlOutput(nameof(ifFalse));

            Requirement(condition, enter);
            Succession(enter, exit);
            Succession(enter, ifTrue);
            Succession(enter, ifFalse);
        }
        
        ControlOutput Enter(Flow flow) {
            flow.Invoke(flow.GetValue<bool>(condition) ? ifTrue : ifFalse);
            return exit;
        }

        IEnumerator EnterCoroutine(Flow flow) {
            yield return flow.GetValue<bool>(condition) ? ifTrue : ifFalse;
            yield return exit;
        }
    }
}