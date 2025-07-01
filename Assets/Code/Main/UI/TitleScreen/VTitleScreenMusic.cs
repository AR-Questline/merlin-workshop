using Awaken.TG.Graphics.Cutscenes;
using Awaken.TG.Main.AudioSystem;
using Awaken.TG.Main.Settings;
using Awaken.TG.Main.Settings.Audio;
using Awaken.TG.Main.UI.Menu;
using Awaken.TG.Main.UI.Menu.OST;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Events;
using Awaken.Utility.Animations;
using UnityEngine;

namespace Awaken.TG.Main.UI.TitleScreen {
    [UsesPrefab("TitleScreen/VTitleScreenMusic")]
    public class VTitleScreenMusic : View<IModel> {
        [SerializeField] ARFmodEventEmitter musicEmitter;
        [SerializeField] ARFmodEventEmitter nonCopyrightedEmitter;
        
        bool AllowCopyrightedMusic => !World.Only<InfluencerMode>().Enabled;

        public override Transform DetermineHost() => Services.Get<ViewHosting>().OnMainCanvas();
        
        protected override void OnInitialize() {
            PlayMusic();
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<Credits>(), this, PauseMusic);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<Credits>(), this, PlayMusic);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelAdded<OstUI>(), this, PauseMusic);
            World.EventSystem.ListenTo(EventSelector.AnySource, World.Events.ModelDiscarded<OstUI>(), this, PlayMusic);
            World.Only<InfluencerMode>().ListenTo(Setting.Events.SettingChanged, PlayMusic, this);
            base.OnInitialize();
        }
        
        protected override IBackgroundTask OnDiscard() {
            PauseMusic();
            return base.OnDiscard();
        }
        
        void PlayMusic() {
            if (AllowCopyrightedMusic) {
                // nonCopyrightedEmitter.Pause();
                // musicEmitter.UnPause(orPlay: true);
            } else {
                // musicEmitter.Pause();
                // nonCopyrightedEmitter.UnPause(orPlay: true);
            }
        }

        public void PauseMusic() {
            // musicEmitter.Pause();
            // nonCopyrightedEmitter.Pause();
        }
    }
}