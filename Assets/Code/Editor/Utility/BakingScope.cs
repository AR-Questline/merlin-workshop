using UnityEditor;

namespace Awaken.TG.Editor.Utility {
    public readonly ref struct BakingScope {
        const string BakingKey = "Baking";

        public static BakingScope Enter() {
            Start();
            return new BakingScope();
        }

        public void Dispose() {
            End();
        }

        public static void Start() => EditorPrefs.SetBool(BakingKey, true);
        
        public static void End() => EditorPrefs.SetBool(BakingKey, false);
    }
}
