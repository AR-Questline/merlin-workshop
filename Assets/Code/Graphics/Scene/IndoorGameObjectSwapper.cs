using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Settings.Graphics;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEngine;

namespace Awaken.TG.Graphics.Scene {
    public class IndoorGameObjectSwapper : StartDependentView<GeneralGraphics> {
        [SerializeField] GameObject outdoor;
        [SerializeField] GameObject indoor;

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, LoadingScreenUI.Events.SceneInitializationEnded, this, OnSceneLoaded);
            OnSceneLoaded();
        }

        void OnSceneLoaded() {
            bool indoors = true;
            if (World.Services.TryGet(out SceneService service)) {
                indoors = !service.IsOpenWorld;
            }

            if (outdoor != null) {
                outdoor.SetActive(!indoors);
            } else {
                Log.Important?.Error($"Outdoor GameObject is not assigned in IndoorGameObjectSwapper on {this}: {(this != null ? gameObject.PathInSceneHierarchy() : null)}", this);
            }

            if (indoor != null) {
                indoor.SetActive(indoors);
            } else {
                Log.Important?.Error($"Indoor GameObject is not assigned in IndoorGameObjectSwapper on {this}: {(this != null ? gameObject.PathInSceneHierarchy() : null)}", this);
            }
        }
    }
}