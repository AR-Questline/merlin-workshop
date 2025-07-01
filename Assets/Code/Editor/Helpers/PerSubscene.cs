using System;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Helpers {
    public static class PerSubscene {
        public static void Action(Action<Scene> action) {
            var subdividedScene = UnityEngine.Object.FindAnyObjectByType<SubdividedScene>();
            if (!subdividedScene) {
                Log.Critical?.Error("No subdivided scene found");
                return;
            }
            var currentScenePath = subdividedScene.gameObject.scene.path;
            using (new BakingScope()) {
                foreach (var subsceneRef in subdividedScene.GetAllScenes(true)) {
                    try {
                        var assetRef = new SceneReference.EditorAccess(subsceneRef).Reference;
                        var path = AssetDatabase.GUIDToAssetPath(assetRef.Address);
                        using var subscene = new SceneResources(path, false);
                        action(subscene.loadedScene);
                    } catch (Exception e) {
                        Log.Critical?.Error($"Exception below happened while processing subscene {subsceneRef.Name}");
                        Debug.LogException(e);
                    }
                }
            }
            EditorSceneManager.OpenScene(currentScenePath);
        }
    }
}
