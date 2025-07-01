using Awaken.TG.MVC.Attributes;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.UI.Handlers.States;
using Awaken.TG.MVC.UI.Universal;

namespace Awaken.TG.Debugging.Cheats {
    [SpawnsView(typeof(VModalBlocker))]
    public partial class GivePlayerCursorElement : Element<CheatController>, IUIStateSource {
        public UIState UIState => UIState.Cursor;
    }
}