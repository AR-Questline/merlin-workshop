using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Awaken.Utility.LowLevel {
    public readonly ref struct LeaksTrackingScope {
        readonly NativeLeakDetectionMode _previousMode;
        readonly bool _forgiveLeaks;

        public LeaksTrackingScope(NativeLeakDetectionMode mode, bool forgiveLeaks) {
            _forgiveLeaks = forgiveLeaks;
            Start(mode, forgiveLeaks, out _previousMode);
        }

        public void Dispose() {
            End(_forgiveLeaks, _previousMode);
        }

        public static void Start(NativeLeakDetectionMode mode, bool forgiveLeaks, out NativeLeakDetectionMode previousMode) {
            previousMode = UnsafeUtility.GetLeakDetectionMode();
            if (forgiveLeaks) {
                UnsafeUtility.CheckForLeaks();
            }
            UnsafeUtility.SetLeakDetectionMode(mode);
        }

        public static void End(bool forgiveLeaks, NativeLeakDetectionMode previousMode) {
            UnsafeUtility.SetLeakDetectionMode(previousMode);
            if (forgiveLeaks) {
                UnsafeUtility.ForgiveLeaks();
            }
        }
    }
}