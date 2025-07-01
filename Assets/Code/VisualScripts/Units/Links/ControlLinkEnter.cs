using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Links {
    [UnitCategory("Links")]
    [TypeIcon(typeof(Flow))]
    public class ControlLinkEnter : Unit, IGraphLink {
        [Serialize, Inspectable, UnitHeaderInspectable] public string label;

        bool _hasCachedOutput;
        ControlOutput _cachedOutput;
        
        public string Label => label;
        
        protected override void Definition() {
            var output = ControlOutput("after");
            ControlInput("", flow => {
                if (GetLinkedOutput() is { } linked) {
                    var stack = flow.PreserveStack();
                    flow.Invoke(linked);
                    flow.RestoreStack(stack);
                    flow.DisposePreservedStack(stack);
                }
                return output;
            });
        }
        
        ControlOutput GetLinkedOutput() {
            if (!_hasCachedOutput) {
                _cachedOutput = RetrieveLinkedOutput();
                _hasCachedOutput = true;
            }
            return _cachedOutput;
        }

        ControlOutput RetrieveLinkedOutput() {
            foreach (var unit in graph.units) {
                if (unit is ControlLinkExit exit) {
                    if (exit.label == label) {
                        return exit.Output;
                    }
                }
            }
            return null;
        }
    }
}