using System.Collections.Generic;
using Awaken.Utility.LowLevel;
using Unity.Profiling;
using UnityEngine.Animations.Rigging;
using UnityEngine.PlayerLoop;

namespace Animancer {
    public static class AnimancerDisposeTracker {
        static readonly ProfilerMarker CheckTrackedAnimancerMarker = new ProfilerMarker("AnimancerDisposeTracker.CheckTrackedAnimancer");
        static readonly ProfilerMarker CheckTrackedRigsMarker = new ProfilerMarker("AnimancerDisposeTracker.CheckTrackedRigs");

        static List<AnimancerComponent> s_trackedAnimancers = new List<AnimancerComponent>(128);
        static List<RigBuilder> s_trackedRigs = new List<RigBuilder>(128);

        public static void Init() {
        }

        public static void StartTracking(AnimancerComponent animancer) {
        }

        public static void StartTracking(RigBuilder rigBuilder) {
        }

        public static void StopTracking(AnimancerComponent animancer) {
        }

        public static void StopTracking(RigBuilder rigBuilder) {
        }

        static void CheckTracked() {
        }
    }
}
