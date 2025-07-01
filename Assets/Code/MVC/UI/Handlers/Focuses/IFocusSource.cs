using UnityEngine;

namespace Awaken.TG.MVC.UI.Handlers.Focuses {
    public interface IFocusSource : IView {
        bool ForceFocus { get; }
        Component DefaultFocus { get; }
    }
}