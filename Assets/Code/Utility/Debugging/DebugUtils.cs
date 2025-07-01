namespace Awaken.Utility.Debugging {
    public static class DebugUtils {
        public static unsafe void Crash() {
            *((int*)null) = 0;
        }
    }
}