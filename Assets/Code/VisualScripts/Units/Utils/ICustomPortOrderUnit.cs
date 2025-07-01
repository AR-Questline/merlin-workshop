using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using Unity.VisualScripting;

namespace Awaken.TG.VisualScripts.Units.Utils {
    public interface ICustomPortOrderUnit : ICustomInputOrderUnit, ICustomOutputOrderUnit { }
    
    public interface ICustomInputOrderUnit : IUnit {
        IEnumerable<IUnitInputPort> CheckedOrderedInputs => OrderedInputs.Select(inputs.MatchingPort).WhereNotNull().Concat(invalidInputs);
        IEnumerable<IUnitInputPort> OrderedInputs { get; }
    }
    
    public interface ICustomOutputOrderUnit : IUnit {
        IEnumerable<IUnitOutputPort> CheckedOrdererOutputs => OrderedOutputs.Select(outputs.MatchingPort).WhereNotNull().Concat(invalidOutputs);
        IEnumerable<IUnitOutputPort> OrderedOutputs { get; }
    }
    
    public static class CustomPortOrderUtils {
        public static T MatchingPort<T>(this IEnumerable<T> ports, T port) where T : class, IUnitPort {
            return port == null ? null : ports.FirstOrDefault(p => p.key == port.key);
        }
    }
}