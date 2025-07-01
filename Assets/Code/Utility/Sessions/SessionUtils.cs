using UnityEngine.Analytics;

namespace Awaken.Utility.Sessions {
    public static class SessionUtils {
        public static long SessionID {
            get {
#if !UNITY_GAMECORE && !UNITY_PS5
                return AnalyticsSessionInfo.sessionId;
#endif
                return 1;
            }
        }
    }
}