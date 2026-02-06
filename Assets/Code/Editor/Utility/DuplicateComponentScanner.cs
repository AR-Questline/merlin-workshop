using System;
using System.Collections.Generic;
using System.IO;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Utility {
    public struct DuplicateResult {
        public string scenePath;
        public string gameObjectName;
        public string gameObjectPath;
        public string componentType;
        public int duplicateCount;
        public int gameObjectInstanceId;
    }

    public class DuplicateComponentScanner {
        // === Constants
        const string DrakeMeshRendererTypeName = "DrakeMeshRenderer";

        public List<string> FindScenesInMultipleFolders(List<string> folderPaths) {
            var scenes = new List<string>();
            var uniqueScenes = new HashSet<string>();

            foreach (var folderPath in folderPaths) {
                if (!Directory.Exists(folderPath)) {
                    continue;
                }

                var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { folderPath });
                foreach (var guid in sceneGuids) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (uniqueScenes.Add(path)) {
                        scenes.Add(path);
                    }
                }
            }

            return scenes;
        }

        public List<DuplicateResult> ScanScene(string scenePath, bool excludeColliders) {
            var results = new List<DuplicateResult>();

            try {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                var rootObjects = scene.GetRootGameObjects();
                var componentList = new List<Component>();

                foreach (var rootObj in rootObjects) {
                    ScanGameObjectHierarchy(rootObj, scenePath, results, componentList, excludeColliders);
                }

                EditorSceneManager.CloseScene(scene, true);
            } catch (Exception ex) {
                Log.Important?.Error($"Error scanning scene {scenePath}: {ex.Message}");
            }

            return results;
        }

        // Recursively scans a GameObject and its children, counting component types
        // to identify duplicate components on the same GameObject
        void ScanGameObjectHierarchy(GameObject obj, string scenePath, List<DuplicateResult> results,
            List<Component> componentList, bool excludeColliders) {

            componentList.Clear();
            obj.GetComponents(componentList);

            var typeDictionary = new Dictionary<Type, int>();
            foreach (var component in componentList) {
                if (component == null) {
                    continue;
                }

                var type = component.GetType();
                if (type == typeof(Transform) || type == typeof(RectTransform)) {
                    continue;
                }

                if (excludeColliders && typeof(Collider).IsAssignableFrom(type)) {
                    continue;
                }

                if (excludeColliders && typeof(Collider2D).IsAssignableFrom(type)) {
                    continue;
                }

                if (type.Name == DrakeMeshRendererTypeName) {
                    continue;
                }

                if (!typeDictionary.ContainsKey(type)) {
                    typeDictionary[type] = 0;
                }
                typeDictionary[type]++;
            }

            foreach (var kvp in typeDictionary) {
                if (kvp.Value > 1) {
                    results.Add(new DuplicateResult {
                        scenePath = scenePath,
                        gameObjectName = obj.name,
                        gameObjectPath = GetGameObjectPath(obj),
                        componentType = kvp.Key.Name,
                        duplicateCount = kvp.Value,
                        gameObjectInstanceId = obj.GetInstanceID()
                    });
                }
            }

            for (int i = 0; i < obj.transform.childCount; i++) {
                ScanGameObjectHierarchy(obj.transform.GetChild(i).gameObject, scenePath, results, componentList, excludeColliders);
            }
        }

        string GetGameObjectPath(GameObject obj) {
            var path = obj.name;
            var parent = obj.transform.parent;

            while (parent != null) {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        public void SelectScene(string scenePath) {
            if (!string.IsNullOrEmpty(scenePath)) {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    Log.Important?.Info($"Opened scene: {scenePath}");
                }
            }
        }

        public void SelectGameObject(DuplicateResult result) {
            if (string.IsNullOrEmpty(result.scenePath)) {
                return;
            }

            var currentScene = SceneManager.GetActiveScene();
            if (currentScene.path != result.scenePath) {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
                    return;
                }
                EditorSceneManager.OpenScene(result.scenePath, OpenSceneMode.Single);
            }

            var obj = FindGameObjectByPath(result.gameObjectPath);
            if (obj != null) {
                Selection.activeGameObject = obj;
                EditorGUIUtility.PingObject(obj);
                SceneView.FrameLastActiveSceneView();
                Log.Important?.Info($"Selected GameObject: {result.gameObjectPath}");
            } else {
                Log.Important?.Warning($"Could not find GameObject: {result.gameObjectPath}");
            }
        }

        // Traverses scene hierarchy by path segments to locate a specific GameObject
        // Returns null if any path segment is not found in the hierarchy
        GameObject FindGameObjectByPath(string path) {
            var parts = path.Split('/');
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            GameObject current = null;

            foreach (var rootObj in rootObjects) {
                if (rootObj.name == parts[0]) {
                    current = rootObj;
                    break;
                }
            }

            if (current == null) {
                return null;
            }

            for (int i = 1; i < parts.Length; i++) {
                var found = false;
                for (int j = 0; j < current.transform.childCount; j++) {
                    var child = current.transform.GetChild(j);
                    if (child.name == parts[i]) {
                        current = child.gameObject;
                        found = true;
                        break;
                    }
                }

                if (!found) {
                    return null;
                }
            }

            return current;
        }
    }
}