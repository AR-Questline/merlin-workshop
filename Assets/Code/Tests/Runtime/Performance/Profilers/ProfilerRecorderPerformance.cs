using Unity.Profiling;

namespace Awaken.Tests.Performance.Profilers {
    public class ProfilerRecorderPerformance : PerformanceProfiler<double> {
        readonly ProfilerCategory _category;
        readonly string _name;
        
        ProfilerRecorder _recorder;

        protected override void OnStart() {
            _recorder = ProfilerRecorder.StartNew(_category, _name);
        }

        protected override void OnEnd() {
            _recorder.Dispose();
        }

        protected override bool TryCapture(out double capture) {
            capture = _recorder.LastValueAsDouble;
            return true;
        }

        public ProfilerRecorderPerformance(ProfilerCategory category, string name) {
            _category = category;
            _name = name;
        }
    }
}