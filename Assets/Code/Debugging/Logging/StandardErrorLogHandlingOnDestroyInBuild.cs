using UnityEngine;

namespace Awaken.TG.Debugging.Logging {
    public class StandardErrorLogHandlingOnDestroyInBuild : MonoBehaviour {
#if !UNITY_EDITOR
        void OnApplicationQuit() {
            new StandardErrorLogHandler().Register();
        }

        void OnDestroy() {
            new StandardErrorLogHandler().Register();
        }
#endif
    }
}