using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Scenes.SubdividedScenes {
    public class SubsceneEditorWindow : EditorWindow {
        SerializedObject _serializedObject;
        
        [MenuItem("TG/Scene Tools/Subscenes")]
        public static void ShowWindow() {
            var window = GetWindow<SubsceneEditorWindow>();
            window.titleContent = new GUIContent("Subscenes");
        }

        void OnEnable() {
            _serializedObject = null;
        }

        void OnGUI() {
            if (Application.isPlaying) {
                EditorGUILayout.LabelField("Works only in edit mode");
                return;
            }

            if (!SubdividedSceneTracker.TryGet(out var scene, out var error)) {
                EditorGUILayout.LabelField(error);
                return;
            }
            
            // Cache serialized object to maintain consistency across frames
            if (_serializedObject == null || _serializedObject.targetObject != scene) {
                _serializedObject = new SerializedObject(scene);
            }
            
            _serializedObject.Update();
            
            SerializedProperty serializedProperty = _serializedObject.FindProperty("serializedSubscenesData");
            
            if (serializedProperty != null) {
                // Create rect with proper window dimensions
                var rect = new Rect(0, 0, position.width, position.height);
                SerializedSubscenesDataDrawer.DrawGUI(rect, serializedProperty);
            }
            
            _serializedObject.ApplyModifiedProperties();
        }
    }
}