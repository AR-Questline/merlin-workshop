using Awaken.TG.Main.Timing;
using Awaken.TG.Main.UI.TitleScreen.Loading;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.Utility;

namespace Awaken.TG.Main.Wyrdnessing {
    public partial class DiscardParentIfNotInWyrdNight : Element<Model> {
        public override ushort TypeForSerialization => SavedModels.DiscardParentIfNotInWyrdNight;

        protected override void OnInitialize() {
            var gameTime = World.Only<GameRealTime>();
            if (!gameTime.WeatherTime.IsNight) {
                World.EventSystem.ListenTo(EventSelector.AnySource, LoadingScreenUI.Events.SceneInitializationEnded, this, DiscardOnSceneInitialization);
            } else {
                gameTime.NightChanged += OnNightChanged;
            }
        }
        
        void OnNightChanged(bool isNight) {
            if (!isNight) {
                ParentModel.Discard();
            }
        }
        
        void DiscardOnSceneInitialization() {
            ParentModel.Discard();
        }

        protected override void OnDiscard(bool fromDomainDrop) {
            var gameTime = World.Any<GameRealTime>();
            if (gameTime) {
                gameTime.NightChanged -= OnNightChanged;
            }
            base.OnDiscard(fromDomainDrop);
        }
    }
}