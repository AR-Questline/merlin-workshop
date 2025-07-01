using Awaken.CommonInterfaces;
using Awaken.TG.Assets;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes {
    public class SubdividedSceneChild : MonoBehaviour, ISubscene {
        public SceneReference SceneRef => SceneReference.ByScene(gameObject.scene);
        public Scene OwnerScene { get; set; }
        
        void Awake() {
            bool isRegistered = TryRegister();
            if (isRegistered == false) {
                return;
            }
            SceneService.SceneLoaded(SceneRef);
            // any initialization code should be nested between this calls
            // however notice that mother scene initialization (e.g. registering services) is done after all child scenes are loaded
            SceneService.SceneInitialized(SceneRef);
        }

        void OnDestroy() {
            Unregister();
        }

        void Unregister() {
            var sceneHandle = gameObject.scene.handle;
            if (ISubscene.SceneHandleToSubsceneMap.TryGetValue(sceneHandle, out var registeredSubscene) && registeredSubscene.Equals(this)) {
                ISubscene.SceneHandleToSubsceneMap.Remove(sceneHandle);
            } else {
                Log.Critical?.Error("Unregistering subscene which was not registered or was unregistered previously");
            }
        }

        bool TryRegister() {
            var sceneHandle = gameObject.scene.handle;
            if (ISubscene.SceneHandleToSubsceneMap.ContainsKey(sceneHandle)) {
                Log.Critical?.Error($"More than one {nameof(SubdividedSceneChild)} in scene {gameObject.scene.name}");
                return false;
            }
            ISubscene.SceneHandleToSubsceneMap.Add(sceneHandle, this);
            return true;
        }
    }
}