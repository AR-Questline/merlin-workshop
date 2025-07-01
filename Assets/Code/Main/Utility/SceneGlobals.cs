using System.Linq;
using Awaken.TG.Main.Scenes.SceneConstructors;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.Utility {
    public static class SceneGlobals {
        public static IMapScene Scene => GetMainScene();

        static IMapScene GetMainScene() {
            Scene activeScene = SceneManager.GetActiveScene();
            IMapScene mainScene = GetMainSceneFrom(activeScene);
            
            // look in other scenes
            if (mainScene == null) {
                int scenesCount = SceneManager.sceneCount;
                int i = 0;
                while (mainScene == null && i < scenesCount) {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded) {
                        mainScene = GetMainSceneFrom(scene);
                    }
                    i++;
                }
            }

            return mainScene;
        }

        static IMapScene GetMainSceneFrom(Scene scene) {
            return scene.GetRootGameObjects().FirstOrDefault(go => go.GetComponent<IMapScene>() != null)?.GetComponent<IMapScene>();
        }
    }
}
