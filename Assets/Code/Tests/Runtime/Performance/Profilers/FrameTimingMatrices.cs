using UnityEngine;

namespace Awaken.Tests.Performance.Profilers {
    public abstract class FrameTimingMatrix : IPerformanceMatrix {
        public IPerformanceProfiler Profiler => FrameTimingPerformance.Instance;
        
        public abstract string Name { get; }
        public abstract double RawDouble(int index);

        protected static FrameTiming GetFrame(int i) => FrameTimingPerformance.Instance.GetFrame(i);
    }
    
    public class FrameTimingCpuTime : FrameTimingMatrix {
        public override string Name => "FT-CPU-ms";
        public override double RawDouble(int index) => GetFrame(index).cpuFrameTime;
    }
    
    public class FrameTimingRenderThreadTime : FrameTimingMatrix {
        public override string Name => "FT-RenderThread-ms";
        public  override double RawDouble(int index) => GetFrame(index).cpuRenderThreadFrameTime;
    }
    
    public class FrameTimingGpuTime : FrameTimingMatrix {
        public override string Name => "FT-GPU-ms";
        public  override double RawDouble(int index) => GetFrame(index).gpuFrameTime;
    }
}