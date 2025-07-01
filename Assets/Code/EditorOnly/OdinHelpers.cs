#if UNITY_EDITOR
using Awaken.Utility.Collections;
using JetBrains.Annotations;
//using Sirenix.OdinValidator.Editor;
using UnityEditor;

namespace Awaken.TG.EditorOnly {
    public static class OdinHelpers {
        [UsedImplicitly]
        public static string Space(int value = 10) {
            EditorGUILayout.Space(value);
            return "";
        }

        [InitializeOnLoadMethod]
        static void AutoValidatorDispose() {
            //EditorApplication.quitting += () => ValidationSession.ActiveValidationSessions.ToArray().ForEach(x => x.Dispose());
        }
    }
}
#endif
