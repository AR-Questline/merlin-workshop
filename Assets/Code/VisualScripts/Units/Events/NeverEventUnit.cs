using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Events {
    [UnitCategory("Events")]
    [UnitOrder(2)]
    [TypeIcon(typeof(CustomEvent))]
    public class NeverEventUnit : Unit {
        [UnityEngine.Scripting.Preserve] ControlOutput _output;

        protected override void Definition() {
            _output = ControlOutput("output");
        }

        public override bool isControlRoot => true;
    }
}