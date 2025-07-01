using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;

namespace Awaken.TG.Main.UI.TitleScreen {
    [SpawnsView(typeof(VPressStartUI))]
    public partial class PressStartUI : Model {
        public override Domain DefaultDomain => Domain.TitleScreen;
        public sealed override bool IsNotSaved => true;
    }
}