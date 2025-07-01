// FROM: https://wiki.unity3d.com/index.php/FindMissingScripts

using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Assets {
    public class FindMissingScriptsRecursivelyWindow : EditorWindow {
        List<GameObject> _withMissing = new List<GameObject>();
        Vector2 _scroll;
        
        [MenuItem("TG/Assets/Find MissingScripts in selected", priority = -100)]
        public static void ShowWindow() {
            GetWindow(typeof(FindMissingScriptsRecursivelyWindow));
        }

        public void OnGUI() {
            if (GUILayout.Button("Find Missing Scripts in selected GameObjects")) {
                _withMissing.Clear();
                FindInSelected();
            }
            if (GUILayout.Button("Select all scene")) {
                Selection.objects = SceneManager.GetActiveScene().GetRootGameObjects().Cast<Object>().ToArray();
            }
            if (_withMissing.IsNotEmpty() && GUILayout.Button("Destroy them")) {
                _withMissing.ForEach(DestroyImmediate);
                _withMissing.Clear();
            }

            if (_withMissing.Count == 0) {
                EditorGUILayout.LabelField("No missing");
            } else if (GUILayout.Button("Remove missing")) {
                foreach (var withMissing in _withMissing) {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(withMissing);
                }
                _withMissing.Clear();
                FindInSelected();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            var oldEnable = GUI.enabled;
            GUI.enabled = false;
            foreach (GameObject gameObject in _withMissing) {
                EditorGUILayout.ObjectField(gameObject, typeof(GameObject), true);
            }
            GUI.enabled = oldEnable;
            EditorGUILayout.EndScrollView();
        }

        void FindInSelected() {
            GameObject[] go = Selection.gameObjects;
            foreach (GameObject g in go) {
                FindInGO(g);
            }
        }

        void FindInGO(GameObject g) {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g) > 0) {
                _withMissing.Add(g);
            }

            foreach (Transform childT in g.transform) {
                FindInGO(childT.gameObject);
            }
        }
    }
}