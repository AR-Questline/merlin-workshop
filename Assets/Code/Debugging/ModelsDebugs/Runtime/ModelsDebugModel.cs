using Awaken.TG.Main.UI.HUD;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Domains;
using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.Debugging.ModelsDebugs.Runtime {
    [SpawnsView(typeof(VModelsDebugModel))]
    public partial class ModelsDebugModel : Model, IUIStateSource {
        public override Domain DefaultDomain => Domain.Globals;
        public sealed override bool IsNotSaved => true;
        public UIState UIState => UIState.ModalState(HUDState.None);
    }
}