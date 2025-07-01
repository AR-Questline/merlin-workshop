using System;

namespace Awaken.TG.Main.UI.Menu {
    public interface ITab {
        bool IsActive { get; }
        void Select();
    }
}