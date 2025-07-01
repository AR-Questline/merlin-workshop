using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Awaken.TG.Editor.AssetManager {
    public class CreateBlendTreeAsset : EditorWindow {
        BlendTree _blendTree;
        AnimatorState _animatorState;

        string _motionName, _newName, _createPath;

        [MenuItem("TG/Animations/Duplicate BlendTree")]
        public static void ShowWindow() {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(CreateBlendTreeAsset));
        }

        void OnGUI() {
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("BlendTree");
            _blendTree = EditorGUILayout.ObjectField(_blendTree, typeof(BlendTree), true) as BlendTree;
            EditorGUILayout.EndHorizontal();

            if (_blendTree) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("BlendTree");
                EditorGUILayout.LabelField(_motionName);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("New Name");
                _newName = EditorGUILayout.TextField(_newName);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Create Path");
                _createPath = EditorGUILayout.TextField(_createPath);
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Duplicate BlendTree")) {
                    int canRun = 0;

                    if (_newName == null || _newName == "") {
                        ShowNotification(new GUIContent("Provide a Name for BlendTree"));
                    } else {
                        canRun++;
                    }

                    if (_createPath == null || _createPath == "") {
                        ShowNotification(new GUIContent("Provide a path for BlendTree"));
                    } else {
                        canRun++;
                    }

                    if (canRun == 2) {
                        BlendTree BTcopy = Instantiate<BlendTree>(_blendTree);

                        AssetDatabase.CreateAsset(BTcopy,
                            AssetDatabase.GenerateUniqueAssetPath(_createPath + _newName + ".asset"));
                    }
                }
            } else {
                setBlendTree();
            }
        }

        void Update() {
            setBlendTree();
        }

        private void setBlendTree() {
            AnimatorState AS = Selection.activeObject as AnimatorState;

            if (AS != null && AS != _animatorState && AS.motion is BlendTree) {
                _animatorState = AS;
                _motionName = _animatorState.name;

                _blendTree = _animatorState.motion as BlendTree;
            }
        }
    }
}