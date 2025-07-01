namespace Awaken.Tests.Performance.Profilers {
    public interface IPerformanceMatrix {
        string Name { get; }
        IPerformanceProfiler Profiler { get; }
        double RawDouble(int index);
    }
}