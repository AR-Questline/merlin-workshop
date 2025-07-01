using System;
using System.Collections.Generic;
using Awaken.Tests.Performance.Preprocessing;
using Awaken.Tests.Performance.Profilers;
using Awaken.Tests.Performance.TestCases;
using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.Tests.Performance {
    public class PerformanceTestRegistry : ScriptableObject {
        static readonly ShareableARAssetReference Reference = new("89913bfcaf8c50449acba6957dceb6d9");
        public static ARAsyncOperationHandle<PerformanceTestRegistry> Load() => Reference.Get().LoadAsset<PerformanceTestRegistry>();

        [SerializeField] SimplePerformanceTestCase[] simpleTests = Array.Empty<SimplePerformanceTestCase>();

        public IReadOnlyList<IPerformancePreprocessor> Preprocessors { get; private set; }
        public IReadOnlyList<IPerformanceTestCase> TestCases { get; private set; }
        public IReadOnlyList<IPerformanceMatrix> Matrices { get; private set; }
        
        public void Init() {
            if (Preprocessors != null) {
                return;
            }
            Preprocessors = new IPerformancePreprocessor[] {
                new PerformanceWeatherPreprocessor(),
                new PerformanceHLODPreprocessor(),
            };
            TestCases = simpleTests;
            Matrices = new IPerformanceMatrix[] {
                new FrameTimingCpuTime(),
                new FrameTimingGpuTime(),
                new FrameTimingRenderThreadTime(),
                new ProfilerRecorderSystemMemory(),
                new ProfilerRecorderGCMemory(),
                new ProfilerRecorderMainThread(),
            };
        }

        #if UNITY_EDITOR
        public struct EDITOR_Accessor {
            PerformanceTestRegistry _registry;
            
            public EDITOR_Accessor(PerformanceTestRegistry registry) {
                _registry = registry;
            }
            
            public ref SimplePerformanceTestCase[] SimpleTests => ref _registry.simpleTests;
        }
        #endif
    }
}