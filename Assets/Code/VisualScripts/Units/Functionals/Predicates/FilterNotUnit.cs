using System;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Functionals.Predicates {
    [TypeIcon(typeof(FlowGraph))]
    [UnitCategory("AR/Utils/Items")]
    public class FilterNotUnit<T> : ARUnit {
        protected override void Definition() {
            var filterPort = RequiredARValueInput<Func<T, bool>>("");
            ValueOutput<Func<T, bool>>("", flow => {
                var filter = filterPort.Value(flow);
                return input => !filter(input);
            });
        }
    }
}