using System.Collections.Generic;
using System.Diagnostics;
using Unity.Profiling;

namespace Awaken.Utility.Profiling {
    public static class ProfilerMarkerUtils {
#if ENABLE_PROFILER
        const int Capacity = 4;
#else 
        const int Capacity = 0;
#endif

        static readonly List<ProfilerRecorderSample> Samples = new(Capacity);
        static readonly Dictionary<int, ProfilerRecorder> RecorderByMarker = new();

        [Conditional("ENABLE_PROFILER")]
        public static void StartRecording(in ProfilerMarker marker) {
            if (RecorderByMarker.TryGetValue(marker.Handle.ToInt32(), out var recorder)) {
                return;
            }
            recorder = ProfilerRecorder.StartNew(marker, Capacity);
            RecorderByMarker.Add(marker.Handle.ToInt32(), recorder);
        }

        [Conditional("ENABLE_PROFILER")]
        public static void StopRecording(in ProfilerMarker marker) {
            if (!RecorderByMarker.Remove(marker.Handle.ToInt32(), out var recorder)) {
                return;
            }
            recorder.Dispose();
        }

        public static double GetTiming(in ProfilerMarker marker) {
#if ENABLE_PROFILER
            if (RecorderByMarker.TryGetValue(marker.Handle.ToInt32(), out var recorder)) {
                return GetRecorderFrameAverage(recorder);
            }
#endif
            return -1;
        }

        static double GetRecorderFrameAverage(ProfilerRecorder recorder) {
            if (recorder.Capacity == 0) {
                return 0;
            }

            double sum = 0;
            recorder.CopyTo(Samples);
            for (var i = 0; i < Samples.Count; ++i) {
                sum += Samples[i].Value;
            }
            var avg = sum / Samples.Count;

            Samples.Clear();

            return avg*1e-6;
        }
    }
}
