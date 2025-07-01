#if UNITY_EDITOR

using UnityEditor;
namespace Awaken.TG.EditorOnly.Utils {
    [InitializeOnLoad]
    public static class EditorApplicationUtils {
        public static bool IsLeavingPlayMode { get; private set; }
        
        static EditorApplicationUtils() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange obj) {
            IsLeavingPlayMode = obj == PlayModeStateChange.ExitingPlayMode;
        }
    }
}
#endif