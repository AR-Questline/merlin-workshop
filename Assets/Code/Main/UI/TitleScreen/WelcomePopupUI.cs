using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;

namespace Awaken.TG.Main.UI.TitleScreen {
    [SpawnsView(typeof(VWelcomePopupUI))]
    public partial class WelcomePopupUI : Element<TitleScreenUI> {
        public sealed override bool IsNotSaved => true;
    }
}