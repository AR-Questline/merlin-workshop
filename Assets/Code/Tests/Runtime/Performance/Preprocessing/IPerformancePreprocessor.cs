using System.Collections.Generic;

namespace Awaken.Tests.Performance.Preprocessing {
    public interface IPerformancePreprocessor {
        string Name { get; }
        IReadOnlyList<IPerformancePreprocessorVariant> Variants { get; }
    }
}