using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.Main.UI.TitleScreen {
    public partial class WaitForInputBoard : Model, IUIStateSource {
        public override Domain DefaultDomain => Domain.Gameplay;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.ModalState(HUDState.MiddlePanelShown).WithPauseTime();
    }
}