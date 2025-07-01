using System;

namespace Awaken.TG.VisualScripts.Units.Functionals.Predicates {
    public class FilterIfUnit<T> : ARUnit {
        protected override void Definition() {
            var conditionInput = RequiredARValueInput<bool>("condition");
            var filterTrueInput = FallbackARValueInput<Func<T, bool>>("filterTrue", _ => _ => true);
            var filterFalseInput = FallbackARValueInput<Func<T, bool>>("filterFalse", _ => _ => true);
            
            ValueOutput("", flow => {
                var filterTrue = filterTrueInput.Value(flow);
                var condition = conditionInput.Value(flow);
                
                if (condition) {
                    return filterTrue;
                }

                return filterFalseInput.Value(flow);
            });
        }
    }
}
