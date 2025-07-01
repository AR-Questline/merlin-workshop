using System.Collections.Generic;

namespace Awaken.Tests.Performance.Preprocessing {
    public class PerformanceHLODPreprocessor : IPerformancePreprocessor {
        readonly Variant[] _variants = {
            new("on"),
            new("off")
        };
        
        public string Name => "HLOD";
        public IReadOnlyList<IPerformancePreprocessorVariant> Variants => _variants;

        class Variant : IPerformancePreprocessorVariant {
            public string Name { get; }
            
            public Variant(string name) {
                Name = name;
            }
            
            public void Process() {
                // TODO: Socha will do sth
            }
        }
    }
}