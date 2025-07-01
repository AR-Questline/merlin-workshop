using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.Utility.Collections;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes {
    /// <summary>
    /// Start a new game.
    /// </summary>
    public class NewGameLoading : ILoadingOperation {
        readonly SceneReference _sceneReference;
        public LoadingType Type => LoadingType.NewGame;
        public SceneReference SceneToLoad => _sceneReference;

        public IEnumerable<SceneReference> ScenesToUnload(SceneReference previousScene) => previousScene.Yield();
        
        public NewGameLoading(SceneReference sceneReference = null) {
            _sceneReference = sceneReference;
        }

        public void DropPreviousDomains(SceneReference _) {
            // Remove Title Screen
            World.DropDomain(Domain.TitleScreen);
            // Remove SaveSlot models, in case the new game was started from in-game (probably never gonna happen)
            World.DropDomain(Domain.SaveSlot);
            // Cleanup cached serialized data
            LoadSave.Get.ClearCache(Domain.SaveSlot);
        }

        public ISceneLoadOperation Load(LoadingScreenUI loadingScreen) {
#if UNITY_EDITOR
            // same key as ProjectValidator.IntendedScene
            const string intendedSceneKey = TitleScreenUtils.IntendedScene;
            SceneReference scene = UnityEditor.EditorPrefs.HasKey(intendedSceneKey)
                ? SceneReference.ByName(UnityEditor.EditorPrefs.GetString(intendedSceneKey))
                : GetInitialScene();
#else
            SceneReference scene = GetInitialScene();
#endif
            var sceneRef = _sceneReference ?? scene;
            var loadingOperation = SceneService.LoadSceneAsync(sceneRef, LoadSceneMode.Additive);
            loadingOperation.OnComplete(() => loadingScreen.NewSceneLoaded(sceneRef));
            return loadingOperation;
        }

        public void OnComplete(IMapScene _) {
            // Construct gameplay from scratch
            GameplayConstructor.CreateGameplay();
        }

        public void Dispose() { }

        static SceneReference GetInitialScene() => World.Services.Get<GameConstants>().initialScene;
    }
}