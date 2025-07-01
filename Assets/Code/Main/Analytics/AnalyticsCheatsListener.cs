#if !UNITY_GAMECORE && !UNITY_PS5
using Awaken.TG.Debugging.Cheats;
using Awaken.TG.Main.Heroes;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;

namespace Awaken.TG.Main.Analytics {
    public partial class AnalyticsCheatsListener : Element<GameAnalyticsController> {
        public sealed override bool IsNotSaved => true;

        protected override void OnInitialize() {
            World.EventSystem.ListenTo(EventSelector.AnySource, Hero.Events.MainViewInitialized, this, UpdateCheats);
            World.Only<CheatController>().ListenTo(Events.AfterChanged, UpdateCheats, this);
            UpdateCheats();
        }

        void UpdateCheats() {
            bool heroCheated = CheatController.CheatsWasEnabledForHero() || CheatController.CheatsEnabled();
            AnalyticsUtils.CheatsEnabled(heroCheated);
        }
    }
}
#endif