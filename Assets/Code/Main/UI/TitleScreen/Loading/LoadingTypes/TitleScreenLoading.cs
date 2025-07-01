using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Saving;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes {
    /// <summary>
    /// Load Title Screen
    /// </summary>
    public class TitleScreenLoading : ILoadingOperation {
        public const string SceneName = "TitleScreen";
        public static readonly SceneReference TitleScreenRef = SceneReference.ByName(SceneName);
        
        public LoadingType Type => LoadingType.Title;
        public SceneReference SceneToLoad => TitleScreenRef;
        public IEnumerable<SceneReference> ScenesToUnload(SceneReference previousScene) {
            SceneService sceneService = World.Services.Get<SceneService>();
            yield return sceneService.AdditiveSceneRef;
            yield return sceneService.MainSceneRef;
        }

        public void DropPreviousDomains(SceneReference _) {
            // Remove SaveSlot models after quitting the game
            World.DropDomain(Domain.SaveSlot);
            // Cleanup cached serialized data
            LoadSave.Get.ClearCache(Domain.SaveSlot);
        }

        public ISceneLoadOperation Load(LoadingScreenUI loadingScreen) {
            SceneLoadOperation loadingOperation = SceneService.LoadSceneAsync(TitleScreenRef, LoadSceneMode.Additive);
            loadingOperation.OnComplete(() => loadingScreen.NewSceneLoaded(TitleScreenRef));
            return loadingOperation;
        }

        public void OnComplete(IMapScene _) { }

        public void Dispose() { }
    }
}