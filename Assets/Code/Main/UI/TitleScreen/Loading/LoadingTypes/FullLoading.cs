using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Saving.SaveSlots;
using Awaken.TG.Main.Scenes;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.AdditiveScenes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Debugging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes {
    /// <summary>
    /// Gets called when the game performs full load of a given save.
    /// Full load includes: loading from title screen, escape menu, quick load, etc.
    /// It drops all save-slot related models and replaces them with loaded ones.
    /// </summary>
    public class FullLoading : ILoadingOperation {
        SaveSlot _slot;
        
        public LoadingType Type => LoadingType.Full;
        public SceneReference SceneToLoad => _slot?.SceneRef;
        public FullLoading(SaveSlot saveSlot) {
            _slot = saveSlot;
        }

        public IEnumerable<SceneReference> ScenesToUnload(SceneReference previousScene) {
            SceneService sceneService = World.Services.Get<SceneService>();
            yield return sceneService.AdditiveSceneRef;
            yield return sceneService.MainSceneRef;
        }

        public void DropPreviousDomains(SceneReference _) {
            // Remove Title Screen
            World.DropDomain(Domain.TitleScreen);
            // Cleanup everything that is left from previous game
            World.DropDomain(Domain.SaveSlot);
            // Cleanup cached serialized data
            LoadSave.Get.ClearCache(Domain.SaveSlot);
            Log.Marking?.Warning($"Dropped previous domains");
        }

        public ISceneLoadOperation Load(LoadingScreenUI loadingScreen) {
            int count = _slot.AdditiveSceneRef != null ? 2 : 1;
            var operations = EnumerateSceneLoading(loadingScreen);
            return new MultiSceneLoadOperation(operations,  _slot.SceneRef.Name, count, false);
        }

        IEnumerable<ISceneLoadOperation> EnumerateSceneLoading(LoadingScreenUI loadingScreen) {
            // Main scene loading
            yield return SafeLoad(loadingScreen, () => {
                LoadSave.Get.LoadSaveSlotToCache(_slot);
                SceneLoadOperation loadOperation = SceneService.LoadSceneAsync(_slot.SceneRef, LoadSceneMode.Additive);
                loadOperation.OnComplete(() => {
                    loadingScreen.RegisterNewScene(_slot.SceneRef);
                    loadingScreen.WaitForSceneInitialization();
                });
                return loadOperation;
            });

            if (_slot.AdditiveSceneRef == null || TitleScreen.wasLoadingFailed != LoadingFailed.False) {
                loadingScreen.CleanupAfterLoading();
            } else {
                // Additive scene loading
                yield return SafeLoad(loadingScreen, () => {
                    ((MapScene) _slot.SceneRef.RetrieveMapScene()).RemoveMainPathfinding();
                    SceneLoadOperation loadOperation = SceneService.LoadSceneAsync(_slot.AdditiveSceneRef, LoadSceneMode.Additive);
                    loadOperation.OnComplete(() => loadingScreen.NewSceneLoaded(_slot.AdditiveSceneRef));
                    return loadOperation;
                });
            }
            
            World.EventSystem.Trigger(SceneLifetimeEvents.Get, SceneLifetimeEvents.Events.OnFullSceneLoaded, null);
        }

        ISceneLoadOperation SafeLoad(LoadingScreenUI loadingScreen, Func<ISceneLoadOperation> func) {
            try {
                return func();
            } catch (Exception e) {
                Log.Important?.Error("Save file corrupted!");
                Debug.LogException(e);
                TitleScreen.wasLoadingFailed = LoadingFailed.SaveFile;
                if (DomainErrorPopup.Displayed) {
                    return new NoSceneLoadOperation();
                }
                SceneLoadOperation loadingOperation = SceneService.LoadSceneAsync(TitleScreenLoading.TitleScreenRef, LoadSceneMode.Additive);
                loadingOperation.OnComplete(() => loadingScreen.NewSceneLoaded(TitleScreenLoading.TitleScreenRef));
                return loadingOperation;
            }
        }

        public void OnComplete(IMapScene mapScene) {
            if (mapScene == null) {
                return;
            }
            if (mapScene is AdditiveScene) {
                mapScene.TryRestoreWorld = () => {
                    if (LoadSave.Get.LoadFromCache(Domain.Scene(_slot.AdditiveSceneRef))) {
                        return true;
                    }
                    Log.Critical?.Error($"Additive map data ({_slot.AdditiveSceneRef?.Name}) not found in save slot {_slot.ID}");
                    mapScene.InitializationCanceled = true;
                    return false;
                };
            } else {
                if (GameplayConstructor.RestoreGameplay(_slot) == false) {
                    mapScene.InitializationCanceled = true;
                    return;
                }
                mapScene.TryRestoreWorld = () => {
                    if (LoadSave.Get.LoadFromCache(Domain.Scene(_slot.SceneRef))) {
                        return true;
                    }
                    Log.Critical?.Error($"Map data ({_slot.SceneRef?.Name}) not found in save slot {_slot.ID}");
                    mapScene.InitializationCanceled = true;
                    return false;
                };
            }
        }

        public void Dispose() {
            
        }
    }
}