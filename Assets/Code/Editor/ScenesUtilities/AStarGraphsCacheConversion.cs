using System;
using System.IO;
using System.Linq;
using Awaken.Utility.Debugging;
using Pathfinding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Path = System.IO.Path;

namespace Awaken.TG.Editor.Utility {
    public static class AStarGraphsCacheConversion {
        [MenuItem("TG/Temporary/Move AStar cache files to StreamingAssets", false, 4000)]
        static void MoveCacheFileToStreamingAssets() {
            var scenesPaths = BuildTools.GetAllScenes();
            MoveCacheFilesToStreamingAssets(scenesPaths);
        }
        
        [MenuItem("TG/Temporary/Move AStar cache files to StreamingAssets in selected scenes", false, 4000)]
        static void MoveCacheFileToStreamingAssetsInSelectedScenes() {
            var scenesPaths = GetsSelectedScenesPaths();
            MoveCacheFilesToStreamingAssets(scenesPaths);
        }
        
        static void MoveCacheFilesToStreamingAssets(string[] scenesPaths) {
            /*string projectFolderPath = System.IO.Path.GetDirectoryName(Application.dataPath) + "/";
            string cacheDirectoryPath = AstarData.PathfindingCacheDirectoryPath;
            System.IO.Directory.CreateDirectory(cacheDirectoryPath);
            for (int i = 0; i < scenesPaths.Length; i++) {
                var scenePath = scenesPaths[i];
                if (TryOpenSceneSingle(scenePath, out var scene)) {
                    var astarPaths = Object.FindObjectsByType(typeof(AstarPath), FindObjectsInactive.Include, FindObjectsSortMode.None);
                    for (int astarPathIndex = 0; astarPathIndex < astarPaths.Length; astarPathIndex++) {
                        MoveCacheFileToStreamingAssets((AstarPath)astarPaths[astarPathIndex], projectFolderPath, cacheDirectoryPath);
                    }
                    EditorSceneManager.SaveScene(scene);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();*/
        }
        
        static bool TryOpenSceneSingle(string scenePath, out Scene scene) {
            if (string.IsNullOrEmpty(scenePath)) {
                scene = default;
                return false;
            }
            try {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) {
                    Log.Minor?.Error($"Scene path {scenePath} is not valid");
                    return false;
                }
                return true;
            } catch (Exception e) {
                Debug.LogException(e);
                scene = default;
                return false;
            }
        }
        
        static string[] GetsSelectedScenesPaths() {
            return Selection.objects.OfType<SceneAsset>().Select(AssetDatabase.GetAssetPath).ToArray();
        }
    }
}