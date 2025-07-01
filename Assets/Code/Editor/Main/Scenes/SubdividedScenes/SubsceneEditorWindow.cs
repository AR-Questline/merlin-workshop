using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Scenes.SubdividedScenes {
    public class SubsceneEditorWindow : EditorWindow {
        
        [MenuItem("TG/Scene Tools/Subscenes")]
        public static void ShowWindow() {
            var window = GetWindow<SubsceneEditorWindow>();
            window.titleContent = new GUIContent("Subscenes");
        }

        void OnGUI() {
            var rect = new Rect(0, 0, Screen.width, Screen.height);
            
            if (Application.isPlaying) {
                EditorGUI.LabelField(rect, "Works only in edit mode");
                return;
            }

            if (!SubdividedSceneTracker.TryGet(out var scene, out var error)) {
                EditorGUI.LabelField(rect, error);
                return;
            }
            
            SerializedObject serializedObject = new(scene);
            SerializedProperty serializedProperty = serializedObject.FindProperty("serializedSubscenesData");
            SerializedSubscenesDataDrawer.DrawGUI(rect, serializedProperty);
        }
    }
}