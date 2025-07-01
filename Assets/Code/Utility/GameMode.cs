#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Awaken.Utility {
    public class GameMode {
#if AR_GAMEMODE_DEMO
        public const bool IsDemo = true;
#elif UNITY_EDITOR
        public static bool IsDemo => EditorPrefs.GetBool("gamemode.demo");
#else
        public const bool IsDemo = false;
#endif
    }
}