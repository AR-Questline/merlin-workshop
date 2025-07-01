using Awaken.TG.MVC;

namespace Awaken.TG.Main.Heroes.HUD {
    public class HeroStatusHUD : StatusHUD {
        protected override void OnAttach() {
            Hero.Current.AfterFullyInitialized(() => Init(Hero.Current));
        }
        
        protected override void OnDiscard() {
            ReleaseListener();
        }
    }
}