using Awaken.TG.MVC.UI.Events;

namespace Awaken.TG.MVC.UI {
    /// <summary>
    /// Core interface for participants in the UI system. Usually implemented by views or
    /// view components, but this is not required.
    /// </summary>
    public interface IUIAware {
        /// <summary>
        /// Called with every UIEvent for which this object is eligible. The object can
        /// ignore it, accept it or prevent it from propagating further.
        /// </summary>
        UIResult Handle(UIEvent evt);
    }
}