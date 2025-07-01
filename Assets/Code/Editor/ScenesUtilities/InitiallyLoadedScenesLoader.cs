using System.Linq;
using Awaken.Utility.Debugging;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Utility {
    public struct InitiallyLoadedScenesLoader {
        public string[] initiallyLoadedScenesPaths;
        public void SaveCurrentScenesAsInitiallyLoaded() {
            initiallyLoadedScenesPaths = new string[SceneManager.sceneCount];
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                initiallyLoadedScenesPaths[i] = SceneManager.GetSceneAt(i).path;
            }
        }

        public void RestoreInitiallyLoadedScenes(string scenePathToExclude = null) {
            if (initiallyLoadedScenesPaths == null || initiallyLoadedScenesPaths.Length == 0) {
                Log.Minor?.Error($"{nameof(InitiallyLoadedScenesLoader)} was not initialized");
                return;
            }
            var lastLoadedScene = SceneManager.GetSceneAt(0);
            for (int i = 0; i < initiallyLoadedScenesPaths.Length; i++) {
                if (scenePathToExclude != null && initiallyLoadedScenesPaths[i] == scenePathToExclude) {
                    continue;
                }
                EditorSceneManager.OpenScene(initiallyLoadedScenesPaths[i], OpenSceneMode.Additive);
            }

            if (!initiallyLoadedScenesPaths.Contains(lastLoadedScene.path)) {
                EditorSceneManager.CloseScene(lastLoadedScene, true);
            }
        }
    }
}