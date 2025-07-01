using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.UI.TitleScreen.Loading.LoadingTypes {
    public interface ILoadingOperation : IDisposable {
        LoadingType Type { get; }

        SceneReference SceneToLoad { get; }
        /// <summary>
        /// Should be called before load
        /// </summary>
        void DropPreviousDomains(SceneReference previousScene);

        IEnumerable<SceneReference> ScenesToUnload(SceneReference previousScene);

        /// <summary>
        /// Operation that initializes scene loading and return handle to it
        /// </summary>
        ISceneLoadOperation Load(LoadingScreenUI loadingScreen);

        /// <summary>
        /// Happens after scene has been loaded
        /// </summary>
        void OnComplete(IMapScene mapScene);
    }
}