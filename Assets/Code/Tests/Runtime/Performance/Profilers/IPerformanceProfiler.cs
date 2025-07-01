namespace Awaken.Tests.Performance.Profilers {
    public interface IPerformanceProfiler {
        void Start();
        void Update();
        void End();
    }
}