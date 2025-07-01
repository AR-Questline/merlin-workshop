using System.Linq;
using System.Threading;
using Awaken.Utility.UI;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Awaken.Utility.Debugging.FrameTimings {
    public class FrameTimingHUDDisplay : UGUIWindowDisplay<FrameTimingHUDDisplay> {
        GUIStyle _style;
        readonly FrameTiming[] _frameTimings = new FrameTiming[20];
        uint _collectedFrameTimings;
        ProfilerRecorder _mainThreadTimeRecorder;

        bool _hasArtificialWorkloadUpdate;
        float _artificialWorkloadUpdate = 1;
        bool _hasArtificialWorkloadLateUpdate;
        float _artificialWorkloadLateUpdate = 1;

        protected override bool WithSearch => false;

        void Awake() {
            _style = new GUIStyle();
            _style.fontSize = 15;
            _style.normal.textColor = Color.white;
            _mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
        }

        void OnDestroy() {
            _mainThreadTimeRecorder.Dispose();
        }

        void Update() {
            CaptureTimings();
            if (_hasArtificialWorkloadUpdate) {
                Thread.Sleep(Mathf.RoundToInt(_artificialWorkloadUpdate));
            }
        }

        void LateUpdate() {
            if (_hasArtificialWorkloadLateUpdate) {
                Thread.Sleep(Mathf.RoundToInt(_artificialWorkloadLateUpdate));
            }
        }

        protected override void DrawWindow() {
            if (_collectedFrameTimings == 0) return;

            PerformanceBottleneck? bottleneck = _frameTimings.Select(DetermineBottleneck)
                .GroupBy(g => g)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            double cpuTime = 0;
            double cpuMainThread = 0;
            double cpuPresent = 0;
            double cpuRenderThread = 0;
            double gpuFrameTime = 0;
            double widthScale = 0;
            double heightScale = 0;
            ulong previousFrameTime = 0;
            ulong toStartSubmitTime = 0;
            ulong submittingTime = 0;

            for (var i = 0; i < _collectedFrameTimings; i++) {
                var frameTiming = _frameTimings[i];
                cpuTime += frameTiming.cpuFrameTime;
                cpuMainThread += frameTiming.cpuMainThreadFrameTime;
                cpuPresent += frameTiming.cpuMainThreadPresentWaitTime;
                cpuRenderThread += frameTiming.cpuRenderThreadFrameTime;
                gpuFrameTime += frameTiming.gpuFrameTime;
                widthScale += frameTiming.widthScale;
                heightScale += frameTiming.heightScale;
                previousFrameTime += frameTiming.cpuTimeFrameComplete - frameTiming.frameStartTimestamp;
                toStartSubmitTime += frameTiming.firstSubmitTimestamp - frameTiming.frameStartTimestamp;
                submittingTime += frameTiming.cpuTimePresentCalled - frameTiming.firstSubmitTimestamp;
            }

            var avgMultiplier = 1.0 / _collectedFrameTimings;
            cpuTime *= avgMultiplier;
            cpuMainThread *= avgMultiplier;
            cpuPresent *= avgMultiplier;
            cpuRenderThread *= avgMultiplier;
            gpuFrameTime *= avgMultiplier;
            widthScale *= avgMultiplier;
            heightScale *= avgMultiplier;
            avgMultiplier /= 10000;
            var previousFrameTimeAvg = previousFrameTime * avgMultiplier;
            var toStartSubmitTimeAvg = toStartSubmitTime * avgMultiplier;
            var submitTingTimeAvg = submittingTime * avgMultiplier;

            double fps = 1000f / cpuTime;
            double memory = Profiler.GetTotalAllocatedMemoryLong() / 1024f / 1024f;
            double totalMemory = Profiler.GetTotalReservedMemoryLong() / 1024f / 1024f;

            var reportMsg =
                $"\nCPU: {cpuTime:00.00} ({fps:00} fps)" +
                $"\nMain Thread: {cpuMainThread:00.00}" +
                $"\nRender Thread: {cpuRenderThread:00.00}" +
                $"\nPresent wait: {cpuPresent:00.00}" +
                $"\nGPU: {gpuFrameTime:00.00}" +
                $"\nPrevious rendering: {previousFrameTimeAvg:.00}" +
                $"\nCpu to start rendering: {toStartSubmitTimeAvg:.00}" +
                $"\nSubmitting: {submitTingTimeAvg:.00}" +
                $"\nUsed memory: {memory:00.00}" +
                $"\nTotal memory: {totalMemory:00.00}" +
                $"\nWidth/Height Scale: {widthScale:00.00}/{heightScale:00.00}" +
                $"\nBottleneck: {bottleneck.ToString()}";
            var renderingStateInfo = new GUIContent($"Multithreading mode: {SystemInfo.renderingThreadingMode}"
#if UNITY_EDITOR
                                                    + $"\nMultithreading: {UnityEditor.PlayerSettings.MTRendering}"
                                                    + $"\nGraphics jobs: {UnityEditor.PlayerSettings.graphicsJobs}-{UnityEditor.PlayerSettings.graphicsJobMode}"
#endif
                );

            GUILayout.Label(reportMsg, _style);
            GUILayout.Label(renderingStateInfo, _style);

            GUILayout.BeginHorizontal();
            _hasArtificialWorkloadUpdate = GUILayout.Toggle(_hasArtificialWorkloadUpdate, "Update workload");
            _artificialWorkloadUpdate = GUILayout.HorizontalSlider(_artificialWorkloadUpdate, 0.01f, 20f);
            GUILayout.Label($"{Mathf.RoundToInt(_artificialWorkloadUpdate)}ms");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _hasArtificialWorkloadLateUpdate = GUILayout.Toggle(_hasArtificialWorkloadLateUpdate, "Late workload");
            _artificialWorkloadLateUpdate = GUILayout.HorizontalSlider(_artificialWorkloadLateUpdate, 0.01f, 20f);
            GUILayout.Label($"{Mathf.RoundToInt(_artificialWorkloadLateUpdate)}ms");
            GUILayout.EndHorizontal();
        }

        void CaptureTimings() {
            FrameTimingManager.CaptureFrameTimings();
            _collectedFrameTimings = FrameTimingManager.GetLatestTimings((uint)_frameTimings.Length, _frameTimings);
        }
        
        static PerformanceBottleneck DetermineBottleneck(FrameTiming s) {
            const float kNearFullFrameTimeThresholdPercent = 0.2f;
            const float kNonZeroPresentWaitTimeMs = 0.5f;

            // If we're on platform which doesn't support GPU time
            if (s.gpuFrameTime == 0)
                return PerformanceBottleneck.Indeterminate;

            double fullFrameTimeWithMargin = (1f - kNearFullFrameTimeThresholdPercent) * s.cpuFrameTime;

            // GPU time is close to frame time, CPU times are not
            if (s.gpuFrameTime > fullFrameTimeWithMargin &&
                s.cpuMainThreadFrameTime < fullFrameTimeWithMargin &&
                s.cpuRenderThreadFrameTime < fullFrameTimeWithMargin)
                return PerformanceBottleneck.GPU;

            // One of the CPU times is close to frame time, GPU is not
            if (s.gpuFrameTime < fullFrameTimeWithMargin &&
                (s.cpuMainThreadFrameTime > fullFrameTimeWithMargin ||
                 s.cpuRenderThreadFrameTime > fullFrameTimeWithMargin))
                return PerformanceBottleneck.CPU;

            // Main thread waited due to Vsync or target frame rate
            if (s.cpuMainThreadPresentWaitTime > kNonZeroPresentWaitTimeMs)
            {
                // None of the times are close to frame time
                if (s.gpuFrameTime < fullFrameTimeWithMargin &&
                    s.cpuMainThreadFrameTime < fullFrameTimeWithMargin &&
                    s.cpuRenderThreadFrameTime < fullFrameTimeWithMargin)
                    return PerformanceBottleneck.PresentLimited;
            }

            return PerformanceBottleneck.Balanced;
        }
        
        enum PerformanceBottleneck {
            Indeterminate,      // Cannot be determined
            PresentLimited,     // Limited by presentation (vsync or framerate cap)
            CPU,                // Limited by CPU (main and/or render thread)
            GPU,                // Limited by GPU
            Balanced,           // Limited by both CPU and GPU, i.e. well balanced
        }
    }
}