namespace Awaken.TG.Utility {
    /// <summary>
    /// Use this if you want to get something from Editor Prefs without using #if defs in your code.
    /// </summary>
    public static class SafeEditorPrefs {
#if UNITY_EDITOR
        public static int GetInt(string key, int fallback = 0) => UnityEditor.EditorPrefs.GetInt(key, fallback);
        public static bool GetBool(string key, bool fallback = false) => UnityEditor.EditorPrefs.GetInt(key, fallback ? 1 : 0) == 1;
        public static float GetFloat(string key, float fallback = 0) => UnityEditor.EditorPrefs.GetFloat(key, fallback);
        public static string GetString(string key, string fallback = null) => UnityEditor.EditorPrefs.GetString(key, fallback);
        public static void SetInt(string key, int value) => UnityEditor.EditorPrefs.SetInt(key, value);
#else
        public static int GetInt(string key, int fallback = 0) => fallback;
        public static bool GetBool(string key, bool fallback = false) => fallback;
        public static float GetFloat(string key, float fallback = 0) => fallback;
        public static string GetString(string key, string fallback = null) => fallback;
        public static void SetInt(string key, int value) {}
#endif
    }
}