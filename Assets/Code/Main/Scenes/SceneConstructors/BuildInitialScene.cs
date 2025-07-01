using Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes;
using Awaken.TG.MVC.Domains;
using UnityEngine;

namespace Awaken.TG.Main.Scenes.SceneConstructors {
    /// <summary>
    /// This scene is only used in build to load TitleScreen from addressables
    /// </summary>
    public class BuildInitialScene : MonoBehaviour {
        void Start() {
            SceneService.LoadSceneAsync(TitleScreenLoading.TitleScreenRef);
        }
    }
}