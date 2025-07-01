using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Pathfinding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes {
    /// <summary>
    /// Move to another map, while still maintaining gameplay models that are not attached to the specific map.
    /// </summary>
    public class MapChangeLoading : ILoadingOperation {
        public LoadingType Type => LoadingType.Map;
        SceneReference SceneRef { get; }
        EventSystem.QueuingHandle _eventQueuing;
        public SceneReference SceneToLoad => SceneRef;
        SceneService SceneService => World.Services.Get<SceneService>();

        public MapChangeLoading(SceneReference sceneRef) {
            SceneRef = sceneRef;
        }

        public void DropPreviousDomains(SceneReference previousScene) {
            if (previousScene == null) return;
            if (previousScene.IsAdditive && !SceneRef.IsAdditive && SceneService.MainSceneRef != SceneRef) {
                // Additive -> Map' (other Map)
                SerializeAndDrop(previousScene.Domain);
                SerializeAndDrop(SceneService.MainSceneRef.Domain);
            } else if (previousScene.IsAdditive || !SceneRef.IsAdditive) {
                // All other cases except for Map -> Additive (it doesn't require unloading)
                SerializeAndDrop(previousScene.Domain);
            } else {
                // Map -> Additive
                ((MapScene)previousScene.RetrieveMapScene()).RemoveMainPathfinding();
            }
        }

        void SerializeAndDrop(Domain domain) {
            // Serialize previous scene
            LoadSave.Get.TrySerialize(domain);
            // Cleanup map-related models
            World.DropDomain(domain);
        }

        public IEnumerable<SceneReference> ScenesToUnload(SceneReference previousScene) {
            if (previousScene == null) yield break;
            if (previousScene.IsAdditive && !SceneRef.IsAdditive && SceneService.MainSceneRef != SceneRef) {
                yield return previousScene;
                yield return SceneService.MainSceneRef;
            } else if (previousScene.IsAdditive || !SceneRef.IsAdditive) {
                yield return previousScene;
            }
        }

        public ISceneLoadOperation Load(LoadingScreenUI loadingScreen) {
            try {
                SceneService service = World.Services.Get<SceneService>();
                if (loadingScreen.PreviousScene != null) {
                    if (loadingScreen.PreviousScene.IsAdditive && SceneRef == service.MainSceneRef) {
                        // Loading back into Map from Additive, no loading required, since it's already loaded
                        ((MapScene)SceneRef.RetrieveMapScene()).RestoreMainPathfinding();
                        AIBase.UnpauseAll();
                        loadingScreen.RegisterNewScene(SceneRef);
                        loadingScreen.OnNothingLoaded();
                        return null;
                    }
                } else if (SceneRef.IsAdditive) {
                    throw new Exception("Launching additive scene from editor is currently not supported");
                }
                
                _eventQueuing = World.EventSystem.EventsQueuing();
                
                SceneLoadOperation loadingOperation = SceneService.LoadSceneAsync(SceneRef, LoadSceneMode.Additive);
                loadingOperation.OnComplete(() => loadingScreen.NewSceneLoaded(SceneRef));
                return loadingOperation;
            } catch (Exception e) {
                Log.Important?.Error("Save file corrupted! (Real Exception below)");
                Debug.LogException(e);
                TitleScreen.wasLoadingFailed = LoadingFailed.SaveFile;
                if (DomainErrorPopup.Displayed) {
                    return new NoSceneLoadOperation();
                }
                return SceneService.LoadSceneAsync(TitleScreenLoading.TitleScreenRef);
            }
        }

        public void OnComplete(IMapScene mapScene) {
            mapScene.TryRestoreWorld = () => {
                var result = LoadSave.Get.LoadFromCache(Domain.Scene(SceneRef));
                _eventQueuing.Dispose();
                _eventQueuing = null;
                return result;
            };
        }

        public void Dispose() {
            
        }
    }
}