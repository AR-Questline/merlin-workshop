using System;
using System.Linq;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.Utility.Editor {
    public static class EditorScenesUtility {
        public static string[] GetsSelectedScenesPaths() {
            return Selection.objects.OfType<SceneAsset>().Select(AssetDatabase.GetAssetPath).ToArray();
        }
        
        public static string[] GetCurrentlyOpenScenesPath() {
            var count = SceneManager.sceneCount;
            string[] scenesPaths = new string[count];
            for (int i = 0; i < count; i++) {
                scenesPaths[i] = SceneManager.GetSceneAt(i).path;
            }

            return scenesPaths;
        }
        
        public static bool TryOpenSceneSingle(string scenePath, out Scene scene) {
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

    }
}