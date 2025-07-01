using System;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Functionals.Predicates {
    [TypeIcon(typeof(FlowGraph))]
    [UnitCategory("AR/Utils/Items")]
    public class FilterAndUnit<T> : ARUnit {
        [Serialize, Inspectable, UnitHeaderInspectable] public int count;
        
        protected override void Definition() {
            var filterPorts = new ARValueInput<Func<T, bool>>[count];
            for (int i = 0; i < count; i++) {
                filterPorts[i] = RequiredARValueInput<Func<T, bool>>("filter_" + i);
            }
            ValueOutput<Func<T, bool>>("", flow => {
                var filters = new Func<T, bool>[count];
                for (int i = 0; i < count; i++) {
                    filters[i] = filterPorts[i].Value(flow);
                }
                return input => {
                    for (int i = 0; i < count; i++) {
                        if (!filters[i](input)) {
                            return false;
                        }
                    }
                    return true;
                };
            });
        }
    }
}