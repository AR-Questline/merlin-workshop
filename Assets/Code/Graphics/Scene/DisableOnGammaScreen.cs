using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.VolumeCheckers;
using Awaken.TG.Main.Settings.Controllers;
using Awaken.TG.Main.Settings.Gameplay;
using Awaken.TG.Main.Settings.GammaSettingScreen;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Graphics.Scene {
    public class DisableOnGammaScreen : StartDependentView<GammaSetting> {
        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<GammaScreen>(), this, OnGammaScreenAdded);
        }

        void OnGammaScreenAdded(Model model) {
            gameObject.SetActive(false);
            model.ListenTo(Model.Events.AfterDiscarded, OnGammaScreenDiscarded, this);
        }

        void OnGammaScreenDiscarded() {
            gameObject.SetActive(true);
        }
    }
}