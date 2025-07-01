using Unity.Profiling;
using UnityEngine;

namespace Awaken.Tests.Performance.Profilers {
    public class FrameTimingPerformance : PerformanceProfiler<FrameTiming> {
        public static readonly FrameTimingPerformance Instance = new();
        
        readonly FrameTiming[] _frameTimings = new FrameTiming[1];
        ProfilerRecorder _mainThreadTimeRecorder;

        protected override bool TryCapture(out FrameTiming capture) {
            FrameTimingManager.CaptureFrameTimings();
            uint captured = FrameTimingManager.GetLatestTimings(1, _frameTimings);
            if (captured > 0) {
                capture = _frameTimings[0];
                return true;
            }
            capture = default;
            return false;
        }

        protected override void OnStart() {
            _mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
        }

        protected override void OnEnd() {
            _mainThreadTimeRecorder.Dispose();
        }
    }
}