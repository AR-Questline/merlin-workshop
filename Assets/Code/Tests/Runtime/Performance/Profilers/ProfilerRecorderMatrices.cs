using Unity.Profiling;

namespace Awaken.Tests.Performance.Profilers {
    public abstract class ProfilerRecorderMatrix : IPerformanceMatrix {
        public const double BytesToMegabytes = 1e-6;
        public const double NanosecondsToMilliseconds = 1e-6;
        
        public abstract string Name { get; }
        public abstract double RawDouble(int index);
        protected abstract ProfilerRecorderPerformance Profiler { get; }
        IPerformanceProfiler IPerformanceMatrix.Profiler => Profiler;
    }
    
    public class ProfilerRecorderSystemMemory : ProfilerRecorderMatrix {
        public override string Name => "PR-SystemMemory-MB";
        protected override ProfilerRecorderPerformance Profiler { get; } = new(ProfilerCategory.Memory, "System Used Memory");
        public override double RawDouble(int index) => Profiler.GetFrame(index) * BytesToMegabytes;
        
    }
    
    public class ProfilerRecorderGCMemory : ProfilerRecorderMatrix {
        public override string Name => "PR-GCMemory-MB";
        protected override ProfilerRecorderPerformance Profiler { get; } = new(ProfilerCategory.Memory, "GC Reserved Memory");
        public override double RawDouble(int index) => Profiler.GetFrame(index) * BytesToMegabytes;
    }

    public class ProfilerRecorderMainThread : ProfilerRecorderMatrix {
        public override string Name => "PR-MainThread-ms";
        protected override ProfilerRecorderPerformance Profiler { get; } = new(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
        public override double RawDouble(int index) => Profiler.GetFrame(index) * NanosecondsToMilliseconds;
    }
}