using System;

namespace Awaken.TG.MVC.UI {
    [Flags]
    public enum UIContext {
        None = 1, 
        Mouse = 2, 
        Keyboard = 4, 
        All = Mouse | Keyboard,
    }
}