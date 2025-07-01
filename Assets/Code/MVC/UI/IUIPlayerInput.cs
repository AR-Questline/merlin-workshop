using System.Collections.Generic;
using Awaken.TG.Main.Utility;

namespace Awaken.TG.MVC.UI {
    public interface IUIPlayerInput : IUIAware {
        IEnumerable<KeyBindings> PlayerKeyBindings { get; }
        int InputPriority => 0;
    }
}