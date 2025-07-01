using System;
using Awaken.TG.MVC.UI;

namespace Awaken.TG.Main.UI.Components.PadShortcuts {
    public interface IShortcutAction {
        bool Active { get; }
        event Action OnActiveChange;
        UIResult Invoke();
    }
}