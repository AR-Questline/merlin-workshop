using Awaken.Utility.Debugging.MemorySnapshots;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    public class KandraMemoryWindow : EditorWindow {
        [MenuItem("TG/Assets/Kandra/Memory")]
        static void ShowWindow() {
            var window = GetWindow<KandraMemoryWindow>();
            window.titleContent = new GUIContent("Kandra memory");
            window.Show();
        }

        void OnEnable() {
            EditorApplication.update -= Repaint;
            EditorApplication.update += Repaint;
        }

        void OnDisable() {
            EditorApplication.update -= Repaint;
        }

        void OnGUI() {
            var instance = KandraRendererManager.Instance;
            if (instance == null) {
                GUILayout.Label("KandraRendererManager is null");
                return;
            }
            GUILayout.Label("Kandra memory", EditorStyles.boldLabel);
            MemorySnapshotMemoryInfo.DrawOnGUI(KandraRendererManager.Instance, false);
        }
    }
}