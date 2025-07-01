using Awaken.TG.Assets;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Graphics.Scene {
    public class DisableIfAdditiveLoaded : StartDependentView<GeneralGraphics> {
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, LoadingScreenUI.Events.SceneInitializationEnded, this, OnSceneLoaded);
            OnSceneLoaded();
        }

        void OnSceneLoaded() {
            // This script should be ignored if placed on additive scene itself
            bool isOnMain = World.Services.Get<SceneService>().MainSceneRef == SceneReference.ByScene(gameObject.scene);
            if (!isOnMain) return;
            
            // Check if there is any additive loaded
            bool isAdditive = World.Services.Get<CommonReferences>().SceneConfigs.IsAdditive();
            gameObject.SetActive(!isAdditive);
        }
    }
}