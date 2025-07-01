using Awaken.TG.MVC.UI.Handlers.States;

namespace Awaken.TG.Main.Utility.UI.RadialMenu {
    public interface IRadialMenuUI : IUIStateSource {
        KeyBindings MainKey { get; }
    }
}