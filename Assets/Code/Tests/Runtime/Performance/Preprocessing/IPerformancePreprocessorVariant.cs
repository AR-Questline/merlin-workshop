namespace Awaken.Tests.Performance.Preprocessing {
    public interface IPerformancePreprocessorVariant {
        string Name { get; }
        void Process();
    }
}