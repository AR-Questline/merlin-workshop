using Awaken.TG.Main.UI.Components.PadShortcuts;

namespace Awaken.TG.MVC.UI.Handlers.States {
    public interface IClosable : IShortcut {
        void Close();
    }
}