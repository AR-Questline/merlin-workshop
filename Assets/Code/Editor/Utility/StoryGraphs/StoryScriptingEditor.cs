using System.Collections.Generic;
using System.IO;
using Awaken.TG.Utility;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Directory = System.IO.Directory;

namespace Awaken.TG.Editor.Utility.StoryGraphs {
    public static class StoryScriptingShortcuts {
        [Shortcut("StoryScripting/Editor/Show Scripting Window", typeof(SceneView), KeyCode.Semicolon)]
        static void ShowScriptingWindow() {
            StoryScripting.Open(Selection.activeGameObject);
        }
    }
    
    public class StoryScripting : EditorWindow {
        static string AbsolutePrefabsPath => Application.dataPath + "/Resources/Data/StoryScripting";

        IEnumerable<GameObject> Prefabs {
            get {
                var prefabs = new List<GameObject>();
                string[] paths = Directory.GetFiles(AbsolutePrefabsPath, "*.prefab", SearchOption.AllDirectories);
                foreach (string prefab in paths) {
                    string assetPath = "Assets" + prefab.Replace(Application.dataPath, "").Replace('\\', '/');
                    GameObject gameObject = (GameObject) AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
                    prefabs.Add(gameObject);
                }
                return prefabs;
            }
        }

        static bool IsOpen { get; set; }
        GameObject _selectedGameObject;
        Vector2 _scrollPos;
        
        public static void Open(GameObject selectedGameObject) {
            if (IsOpen || selectedGameObject == null) {
                return;
            }
            StoryScripting storyScripting = CreateInstance<StoryScripting>();
            storyScripting._selectedGameObject = selectedGameObject;
            storyScripting.titleContent = new GUIContent("Story Scripting");
            Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
            Rect position = new(mousePos, Vector2.zero);
            storyScripting.ShowAsDropDown(position, new Vector2(300, 100));
        }

        void OnGUI() {
            EditorGUILayout.BeginVertical();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            var skin =  GUI.skin;
            GUIStyle buttonStyle = new(skin.button) {
                richText = true
            };
            
            foreach (GameObject prefab in Prefabs) {
                string buttonName = _selectedGameObject.isStatic
                    ? prefab.name
                    : $"<color=#999999>{prefab.name}</color>\n<color=#FF0000>Selected GameObject is not static!</color>";
                if (GUILayout.Button(buttonName, buttonStyle) && _selectedGameObject.isStatic) {
                    AttachPrefabToGameObject(prefab);
                    Close();
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void AttachPrefabToGameObject(GameObject prefab) {
            GameObject newPrefabInstance = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
            newPrefabInstance.transform.SetParent(_selectedGameObject.transform);
            newPrefabInstance.transform.localPosition = Vector3.zero;
            EditorUtility.SetDirty(_selectedGameObject);
            Scene scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = newPrefabInstance.transform;

            Bounds bounds = TransformBoundsUtil.FindBounds(_selectedGameObject.transform, false);
            BoxCollider trigger = newPrefabInstance.GetComponentInChildren<BoxCollider>();
            trigger.transform.position = bounds.center;
            trigger.size = bounds.size;
        }

        void OnEnable() {
            IsOpen = true;
        }

        void OnDestroy() {
            IsOpen = false;
        }
    }
}


