using Awaken.TG.Main.VisualGraphUtils;
using Awaken.TG.VisualScripts.Units.Utils;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.General {
    [UnitCategory("AR/General")]
    [TypeIcon(typeof(FlowGraph))]
    public class CoroutineUnit : Unit {
        ControlInput _enter;
        ControlOutput _exit;
        
        protected override void Definition() {
            _enter = ControlInput("enter", Enter);
            _exit = ControlOutput("exit");
            Succession(_enter, _exit);
        }

        ControlOutput Enter(Flow flow) {
            var coroutineFlow = AutoDisposableFlow.New(flow.stack.AsReference());
            VGUtils.CopyFlowVariables(flow, coroutineFlow.flow);
            coroutineFlow.flow.StartCoroutine(_exit);
            return null;
        }
    }

    [UnitCategory("AR/General")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class CoroutineWithVariableUnit : Unit {
        [Serialize, Inspectable, UnitHeaderInspectable] public int count;
        
        ControlInput _enter;
        ControlOutput _exit;

        ValueInput[] _inputs;
        ValueOutput[] _outputs;
        
        protected override void Definition() {
            _enter = ControlInput("enter", Enter);
            _exit = ControlOutput("exit");

            _inputs = new ValueInput[count];
            _outputs = new ValueOutput[count];

            for (int i = 0; i < count; i++) {
                _inputs[i] = ValueInput<object>($"in_{i}");
                _outputs[i] = ValueOutput<object>($"out_{i}");
            }
            
            Succession(_enter, _exit);
        }

        ControlOutput Enter(Flow flow) {
            var coroutineFlow = AutoDisposableFlow.New(flow.stack.AsReference());
            
            VGUtils.CopyFlowVariables(flow, coroutineFlow.flow);
            for (int i = 0; i < count; i++) {
                coroutineFlow.flow.SetValue(_outputs[i], flow.GetValue(_inputs[i]));
            }
            
            coroutineFlow.flow.StartCoroutine(_exit);
            
            return null;
        }
    }
}